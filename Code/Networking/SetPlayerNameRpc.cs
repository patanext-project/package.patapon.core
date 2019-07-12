using package.stormiumteam.shared.ecs;
using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace P4.Core.Code.Networking
{
	public struct SetPlayerNameRpc : IRpcCommand
	{
		public int            ServerId;
		public NativeString64 Name;

		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			var entity = commandBuffer.CreateEntity(jobIndex);
			commandBuffer.AddComponent(jobIndex, entity, new SetPlayerNamePayload {ServerId = ServerId, Connection = connection, Name = Name});
		}

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(ServerId);
			writer.Write(Name.Length);
			for (var i = 0; i != Name.Length; i++)
			{
				writer.Write((uint) Name[i]);
			}
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			ServerId = reader.ReadInt(ref ctx);
			Name     = new NativeString64 {Length = reader.ReadInt(ref ctx)};
			for (var i = 0; i != Name.Length; i++)
			{
				Name[i] = (char) reader.ReadUInt(ref ctx);
			}
		}
	}

	public struct PlayerName : IComponentData
	{
		public NativeString64 Value;
	}

	public struct SetPlayerNamePayload : IComponentData
	{
		public int            ServerId;
		public Entity         Connection;
		public NativeString64 Name;
	}

	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[UpdateAfter(typeof(CreateGamePlayerSystem))]
	public class SetPlayerNameSystem : GameBaseSystem
	{
		private EntityQuery m_Query;
		private EntityQuery m_PlayerQuery;
		private EntityQuery m_NewConnections;

		private NativeList<Entity> DelaySendNameEntities;

		public NetworkConnectionModule ConnectionModule;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Query          = GetEntityQuery(typeof(SetPlayerNamePayload));
			m_PlayerQuery    = GetEntityQuery(typeof(PlayerDescription));
			m_NewConnections = GetEntityQuery(typeof(CreateGamePlayer));
			
			DelaySendNameEntities = new NativeList<Entity>(Allocator.Persistent);

			GetModule(out ConnectionModule);
		}

		protected override void OnUpdate()
		{
			EntityManager.CompleteAllJobs();

			ConnectionModule.Update(default);

			var payloadEntities = m_Query.ToEntityArray(Allocator.TempJob);
			var payloadArray    = m_Query.ToComponentDataArray<SetPlayerNamePayload>(Allocator.TempJob);
			var playerEntities  = m_PlayerQuery.ToEntityArray(Allocator.TempJob);
			if (IsServer)
			{
				var rpcQueue = World.GetOrCreateSystem<RpcQueueSystem<SetPlayerNameRpc>>().GetRpcQueue();

				var newConnections = m_NewConnections.ToEntityArray(Allocator.TempJob);
				for (var con = 0; con != newConnections.Length; con++)
				{
					DelaySendNameEntities.Add(EntityManager.GetComponentData<CreateGamePlayer>(newConnections[con]).Connection);
				}
				newConnections.Dispose();
				
				for (var ent = 0; ent != payloadArray.Length; ent++)
				{
					var playerIdx = -1;
					for (var p = 0; playerIdx == -1 && p != playerEntities.Length; p++)
					{
						if (!EntityManager.HasComponent<NetworkOwner>(playerEntities[p]))
							continue;

						var networkOwner = EntityManager.GetComponentData<NetworkOwner>(playerEntities[p]);
						if (networkOwner.Value == payloadArray[ent].Connection)
							playerIdx = p;
					}

					if (playerIdx < 0)
					{
						Debug.LogError("(Server) No player found with connection=" + payloadArray[ent].Connection);
						continue;
					}

					EntityManager.SetOrAddComponentData(playerEntities[playerIdx], new PlayerName {Value = payloadArray[ent].Name});

					for (var con = 0; con != ConnectionModule.ConnectedEntities.Length; con++)
					{
						var outgoingData = EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(ConnectionModule.ConnectedEntities[con]);
						var serverId     = EntityManager.GetComponentData<NetworkIdComponent>(payloadArray[ent].Connection);

						rpcQueue.Schedule(outgoingData, new SetPlayerNameRpc {ServerId = serverId.Value, Name = payloadArray[ent].Name});
					}
				}
				
				for (var con = 0; con != DelaySendNameEntities.Length; con++)
				{
					if (!EntityManager.HasComponent<OutgoingRpcDataStreamBufferComponent>(DelaySendNameEntities[con]))
						continue;
					
					var outgoingData = EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(DelaySendNameEntities[con]);
					foreach (var player in playerEntities)
					{
						if (!EntityManager.HasComponent<PlayerName>(player))
							continue;
							
						var serverId = EntityManager.GetComponentData<GamePlayer>(player).ServerId;
						var name     = EntityManager.GetComponentData<PlayerName>(player);
						
						rpcQueue.Schedule(outgoingData, new SetPlayerNameRpc {ServerId = serverId, Name = name.Value});
					}
					
					DelaySendNameEntities.RemoveAtSwapBack(con);
					con--;
				}

				EntityManager.DestroyEntity(m_Query);
			}
			else
			{
				for (var ent = 0; ent != payloadArray.Length; ent++)
				{
					var playerIdx = -1;
					for (var p = 0; playerIdx == -1 && p != playerEntities.Length; p++)
					{
						var isLocal = EntityManager.HasComponent(playerEntities[p], ComponentType.ReadWrite<GamePlayerLocalTag>());
						var data    = EntityManager.GetComponentData<GamePlayer>(playerEntities[p]);
						if (data.ServerId == payloadArray[ent].ServerId)
						{
							playerIdx = isLocal ? -2 : p;
						}
					}

					// not found...
					if (playerIdx == -1)
					{
						continue;
					}

					EntityManager.DestroyEntity(payloadEntities[ent]);

					// local player
					if (playerIdx == -2)
						continue;

					EntityManager.SetOrAddComponentData(playerEntities[playerIdx], new PlayerName {Value = payloadArray[ent].Name});
				}
			}

			payloadEntities.Dispose();
			payloadArray.Dispose();
			playerEntities.Dispose();
		}
	}
}