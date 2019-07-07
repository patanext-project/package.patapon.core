using System;
using System.Collections.Generic;
using package.patapon.core;
using package.stormiumteam.shared.ecs;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon4TLB.Default
{
	public struct RequestCreateCommand : IComponentData
	{
		public int ChainId;
		public int TypeId;

		public RhythmCommandData CommandData;
	}

	public struct CommandChainGroup : IComponentData
	{
		public int Value;
	}

	public struct FlowClientCheckCommandTag : IComponentData
	{
	}

	public struct RhythmRpcServerSendCommandChain : IRpcCommand
	{
		public bool                               IsValid;
		public NativeArray<RhythmCommandSequence> ResultBuffer;
		public RhythmCommandData                  CommandData;

		public int ChainId;
		public int TypeId;

		public NetworkCompressionModel CompressionModel;

		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			if (!IsValid || !ResultBuffer.IsCreated)
			{
				throw new Exception();
			}

			var ent = commandBuffer.CreateEntity(jobIndex);
			commandBuffer.AddComponent(jobIndex, ent, new RequestCreateCommand
			{
				ChainId = ChainId,
				TypeId  = TypeId,

				CommandData = CommandData
			});

			var buffer = commandBuffer.AddBuffer<RhythmCommandSequenceContainer>(jobIndex, ent);

			buffer.Reinterpret<RhythmCommandSequence>().CopyFrom(ResultBuffer);
		}

		public void Serialize(DataStreamWriter writer)
		{
			using (CompressionModel = new NetworkCompressionModel(Allocator.Temp))
			{
				writer.WritePackedInt(ChainId, CompressionModel);
				writer.WritePackedInt(TypeId, CompressionModel);
				writer.WritePackedInt(ResultBuffer.Length, CompressionModel);
				for (var i = 0; i != ResultBuffer.Length; i++)
				{
					writer.WritePackedInt(ResultBuffer[i].Key, CompressionModel);
					writer.WritePackedInt(ResultBuffer[i].BeatRange.start, CompressionModel);
					writer.WritePackedInt(ResultBuffer[i].BeatRange.length, CompressionModel);
				}

				writer.WritePackedInt(CommandData.Identifier.Length, CompressionModel);
				for (var c = 0; c != CommandData.Identifier.Length; c++)
				{
					writer.WritePackedUInt(CommandData.Identifier[c], CompressionModel);
				}

				writer.WritePackedInt(CommandData.BeatLength, CompressionModel);
			}
			writer.Flush();
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			using (CompressionModel = new NetworkCompressionModel(Allocator.Temp))
			{
				IsValid = true;
				ChainId = reader.ReadPackedInt(ref ctx, CompressionModel);
				TypeId  = reader.ReadPackedInt(ref ctx, CompressionModel);

				ResultBuffer = new NativeArray<RhythmCommandSequence>(reader.ReadPackedInt(ref ctx, CompressionModel), Allocator.Temp);
				for (var i = 0; i != ResultBuffer.Length; i++)
				{
					ResultBuffer[i] = new RhythmCommandSequence
					{
						Key       = reader.ReadPackedInt(ref ctx, CompressionModel),
						BeatRange = new RangeInt(reader.ReadPackedInt(ref ctx, CompressionModel), reader.ReadPackedInt(ref ctx, CompressionModel))
					};
				}

				var idLength     = reader.ReadPackedInt(ref ctx, CompressionModel);
				var nativeString = new NativeString64 {Length = idLength};
				for (var c = 0; c != idLength; c++)
				{
					nativeString[c] = (char) reader.ReadPackedUInt(ref ctx, CompressionModel);
				}

				CommandData = new RhythmCommandData {Identifier = nativeString, BeatLength = reader.ReadPackedInt(ref ctx, CompressionModel)};
			}
		}
	}

	[DisableAutoCreation]
	public class RhythmCommandManager : ComponentSystem
	{
		public NativeHashMap<int, Entity> CommandIdToEntity;
		public NativeHashMap<Entity, int> EntityToCommandId;

		protected override void OnCreate()
		{
			CommandIdToEntity = new NativeHashMap<int, Entity>(8, Allocator.Persistent);
			EntityToCommandId = new NativeHashMap<Entity, int>(8, Allocator.Persistent);
		}

		protected override void OnUpdate()
		{

		}

		protected override void OnDestroy()
		{
			CommandIdToEntity.Dispose();
			EntityToCommandId.Dispose();
		}

		public void UpdateCommands(NativeArray<Entity> entities)
		{
			CommandIdToEntity.Clear();
			EntityToCommandId.Clear();
			foreach (var entity in entities)
			{
				var commandId = EntityManager.GetComponentData<RhythmCommandId>(entity).Value;

				CommandIdToEntity.TryAdd(commandId, entity);
				EntityToCommandId.TryAdd(entity, commandId);
			}
		}
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class RhythmRpcServerSendCommandSystem : ComponentSystem
	{
		private EntityQuery m_Commands;
		private EntityQuery m_Connections;
		private EntityQuery m_NewConnections;

		private int m_PreviousLength;
		private int m_LastChainId;

		private List<RhythmRpcServerSendCommandChain> m_RpcGroup;
		private RhythmCommandManager m_CommandManager;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_CommandManager = World.GetOrCreateSystem<RhythmCommandManager>();
			m_Commands       = GetEntityQuery(typeof(RhythmCommandId), typeof(RhythmCommandData), typeof(RhythmCommandSequenceContainer));
			m_Connections    = GetEntityQuery(typeof(NetworkStreamInGame), ComponentType.Exclude<NetworkStreamDisconnected>());
			m_NewConnections = GetEntityQuery(typeof(CreateGamePlayer));
			m_LastChainId    = 1;

			m_RpcGroup = new List<RhythmRpcServerSendCommandChain>();
		}

		private void SendToConnections(NativeArray<Entity> connections, RhythmRpcServerSendCommandChain rpc)
		{
			var queue = World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcServerSendCommandChain>>().GetRpcQueue();
			for (var ent = 0; ent != connections.Length; ent++)
			{
				queue.Schedule(EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(connections[ent]), rpc);
			}
		}

		private void SendToConnections(NativeArray<CreateGamePlayer> connections, RhythmRpcServerSendCommandChain rpc)
		{
			var queue = World.GetOrCreateSystem<RpcQueueSystem<RhythmRpcServerSendCommandChain>>().GetRpcQueue();
			for (var ent = 0; ent != connections.Length; ent++)
			{
				queue.Schedule(EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(connections[ent].Connection), rpc);
			}
		}

		protected override void OnUpdate()
		{
			var sameLength = m_PreviousLength == m_Commands.CalculateLength();
			var newPlayers = m_NewConnections.CalculateLength() > 0;
			if (m_PreviousLength == m_Commands.CalculateLength()
			    && !newPlayers)
				return;

			if (!sameLength)
			{
				m_PreviousLength = m_Commands.CalculateLength();
				EntityManager.CompleteAllJobs();

				m_LastChainId++;

				foreach (var rpc in m_RpcGroup)
				{
					rpc.ResultBuffer.Dispose();
				}

				m_RpcGroup.Clear();

				var commands    = m_Commands.ToEntityArray(Allocator.TempJob);
				var connections = m_Connections.ToEntityArray(Allocator.TempJob);
				for (var com = 0; com != commands.Length; com++)
				{
					var rpc = new RhythmRpcServerSendCommandChain
					{
						ChainId      = m_LastChainId,
						IsValid      = true,
						TypeId       = EntityManager.GetComponentData<RhythmCommandId>(commands[com]).Value,
						ResultBuffer = EntityManager.GetBuffer<RhythmCommandSequenceContainer>(commands[com]).Reinterpret<RhythmCommandSequence>().ToNativeArray(Allocator.Persistent),
						CommandData  = EntityManager.GetComponentData<RhythmCommandData>(commands[com])
					};

					m_RpcGroup.Add(rpc);
					SendToConnections(connections, rpc);
				}
				
				// Update Rhythm Command Manager
				m_CommandManager.UpdateCommands(commands);

				commands.Dispose();
				connections.Dispose();
			}
			else
			{
				var connections = m_NewConnections.ToComponentDataArray<CreateGamePlayer>(Allocator.TempJob);
				foreach (var rpc in m_RpcGroup)
				{
					SendToConnections(connections, rpc);
				}

				connections.Dispose();
			}
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class RhythmRpcClientReceiveCommandSystem : ComponentSystem
	{
		private EntityQuery m_CommandRequest;
		private EntityQuery m_CurrentCommands;

		private RhythmCommandManager m_CommandManager;
		
		private int m_LastChainId;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_CommandManager = World.GetOrCreateSystem<RhythmCommandManager>();
			m_CommandRequest  = GetEntityQuery(typeof(RequestCreateCommand));
			m_CurrentCommands = GetEntityQuery(typeof(RhythmCommandId), typeof(RhythmCommandData), typeof(RhythmCommandSequenceContainer), typeof(CommandChainGroup));
			m_LastChainId     = -1;
		}

		protected override void OnUpdate()
		{
			if (m_CommandRequest.CalculateLength() <= 0)
				return;

			EntityManager.CompleteAllJobs();

			var validEntities = new NativeList<Entity>(Allocator.TempJob);
			using (var entities = m_CommandRequest.ToEntityArray(Allocator.TempJob))
			{
				var newChainId = -1;
				for (var ent = 0; ent != entities.Length; ent++)
				{
					var request = EntityManager.GetComponentData<RequestCreateCommand>(entities[ent]);

					if (request.ChainId >= newChainId) // it may be possible (extremely rare, 0.000001% chance) that the server could send two or more groups of commands. 
					{
						newChainId = request.ChainId;
						validEntities.Add(entities[ent]);
					}
				}

				m_LastChainId = newChainId;
			}

			using (var entities = m_CurrentCommands.ToEntityArray(Allocator.TempJob))
			{
				for (var ent = 0; ent != entities.Length; ent++)
				{
					// destroy entities that are not present in the current chain
					if (EntityManager.GetComponentData<CommandChainGroup>(entities[ent]).Value != m_LastChainId)
						EntityManager.DestroyEntity(entities[ent]);
				}
			}

			var builder = World.GetOrCreateSystem<RhythmCommandBuilder>();
			var cmdEntityArray = new NativeArray<Entity>(validEntities.Length, Allocator.Temp);
			for (var ent = 0; ent != validEntities.Length; ent++)
			{
				var requestData = EntityManager.GetComponentData<RequestCreateCommand>(validEntities[ent]);
				var requestBuffer = EntityManager.GetBuffer<RhythmCommandSequenceContainer>(validEntities[ent])
				                                 .Reinterpret<RhythmCommandSequence>()
				                                 .ToNativeArray(Allocator.TempJob);

				var cmdEntity = builder.GetOrCreate(requestBuffer, true);
				EntityManager.SetOrAddComponentData(cmdEntity, new FlowClientCheckCommandTag());
				EntityManager.SetOrAddComponentData(cmdEntity, new CommandChainGroup());
				EntityManager.SetOrAddComponentData(cmdEntity, new RhythmCommandData());

				EntityManager.SetComponentData(cmdEntity, new RhythmCommandId {Value   = requestData.TypeId});
				EntityManager.SetComponentData(cmdEntity, new CommandChainGroup {Value = requestData.ChainId});
				EntityManager.SetComponentData(cmdEntity, requestData.CommandData);

				var cmdBuffer = EntityManager.GetBuffer<RhythmCommandSequenceContainer>(cmdEntity);
				cmdBuffer.CopyFrom(EntityManager.GetBuffer<RhythmCommandSequenceContainer>(validEntities[ent]));

				cmdEntityArray[ent] = cmdEntity;
			}
			
			// Update Rhythm Command Manager
			m_CommandManager.UpdateCommands(cmdEntityArray);

			validEntities.Dispose();
			cmdEntityArray.Dispose();

			EntityManager.DestroyEntity(m_CommandRequest);
		}
	}
}