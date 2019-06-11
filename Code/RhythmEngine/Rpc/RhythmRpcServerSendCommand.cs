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
		public bool                             IsValid;
		public NativeArray<RhythmCommandSequence> ResultBuffer;
		public RhythmCommandData CommandData;

		public int ChainId;
		public int TypeId;

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
			writer.Write(ChainId);
			writer.Write(TypeId);
			writer.Write(ResultBuffer.Length);
			for (var i = 0; i != ResultBuffer.Length; i++)
			{
				writer.Write(ResultBuffer[i].Key);
				writer.Write(ResultBuffer[i].BeatRange.start);
				writer.Write(ResultBuffer[i].BeatRange.length);
			}

			writer.Write(CommandData.BeatLength);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			IsValid = true;
			ChainId = reader.ReadInt(ref ctx);
			TypeId  = reader.ReadInt(ref ctx);

			ResultBuffer = new NativeArray<RhythmCommandSequence>(reader.ReadInt(ref ctx), Allocator.Temp);
			for (var i = 0; i != ResultBuffer.Length; i++)
			{
				ResultBuffer[i] = new RhythmCommandSequence
				{
					Key       = reader.ReadInt(ref ctx),
					BeatRange = new RangeInt(reader.ReadInt(ref ctx), reader.ReadInt(ref ctx))
				};
			}

			CommandData = new RhythmCommandData {BeatLength = reader.ReadInt(ref ctx)};
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

		protected override void OnCreate()
		{
			base.OnCreate();

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

		private int m_LastChainId;

		protected override void OnCreate()
		{
			base.OnCreate();

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
			for (var ent = 0; ent != validEntities.Length; ent++)
			{
				/*var cmdEntity = EntityManager.CreateEntity(
					typeof(FlowCommandId),
					typeof(FlowCommandData),
					typeof(FlowCommandSequenceContainer),
					typeof(FlowClientCheckCommandTag),
					typeof(CommandChainGroup)
				);*/

				var requestData   = EntityManager.GetComponentData<RequestCreateCommand>(validEntities[ent]);
				var requestBuffer = EntityManager.GetBuffer<RhythmCommandSequenceContainer>(validEntities[ent])
				                                 .Reinterpret<RhythmCommandSequence>()
				                                 .ToNativeArray(Allocator.TempJob);

				var cmdEntity = builder.GetOrCreate(requestBuffer, true);
				EntityManager.SetOrAddComponentData(cmdEntity, new FlowClientCheckCommandTag());
				EntityManager.SetOrAddComponentData(cmdEntity, new CommandChainGroup());
				EntityManager.SetOrAddComponentData(cmdEntity, new RhythmCommandData());
				
				EntityManager.SetComponentData(cmdEntity, new RhythmCommandId {Value     = requestData.TypeId});
				EntityManager.SetComponentData(cmdEntity, new CommandChainGroup {Value = requestData.ChainId});
				EntityManager.SetComponentData(cmdEntity, new RhythmCommandData {BeatLength = requestData.CommandData.BeatLength});

				var cmdBuffer = EntityManager.GetBuffer<RhythmCommandSequenceContainer>(cmdEntity);
				cmdBuffer.CopyFrom(EntityManager.GetBuffer<RhythmCommandSequenceContainer>(validEntities[ent]));
			}

			validEntities.Dispose();

			EntityManager.DestroyEntity(m_CommandRequest);
		}
	}
}