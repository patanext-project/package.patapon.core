using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon4TLB.Core
{
	public struct PlayerConnectedRpc : IRpcCommand
	{
		public int ServerId;

		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			var delayed = commandBuffer.CreateEntity(jobIndex);
			commandBuffer.AddComponent(jobIndex, delayed, new DelayedPlayerConnection {ServerId = ServerId});
		}

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(ServerId);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			ServerId = reader.ReadInt(ref ctx);
		}
	}

	/* --------------------------------------------------------- *
	 * We wait for the snapshot to come to spawn GamePlayer ghosts.
	 * Once it's done, we collect all DelayedConnectEvent and
	 * create a ConnectEvent.
	 */
	public struct DelayedPlayerConnection : IComponentData
	{
		public int ServerId;
	}

	public struct PlayerConnectedEvent : IComponentData
	{
		public Entity Player;
		public int    ServerId;
	}

	public struct GamePlayerReadyTag : IComponentData
	{
	}

	public struct GamePlayerLocalTag : IComponentData
	{
	}

	[UpdateInGroup(typeof(GhostSpawnSystemGroup))]
	[UpdateAfter(typeof(GamePlayerGhostSpawnSystem))]
	public class PlayerConnectedEventCreationSystem : JobComponentSystem
	{
		[BurstCompile]
		public struct FindFirstNetworkIdJob : IJobForEach<NetworkIdComponent>
		{
			public NativeArray<NetworkIdComponent> PlayerIds;

			[BurstDiscard]
			private void NonBurst_ThrowWarning()
			{
				Debug.LogWarning("PlayerIds[0] already assigned to " + PlayerIds[0].Value);
			}

			public void Execute(ref NetworkIdComponent networkId)
			{
				if (PlayerIds[0].Value == default)
				{
					PlayerIds[0] = networkId;
				}
				else
				{
					NonBurst_ThrowWarning();
				}
			}
		}

		[RequireComponentTag(typeof(ReplicatedEntityComponent))]
		public struct FindPlayerJob : IJobForEachWithEntity<GamePlayer>
		{
			[ReadOnly]
			public ComponentDataFromEntity<GamePlayerReadyTag> PlayerReadyTag;

			[ReadOnly, DeallocateOnJobCompletion]
			public NativeArray<NetworkIdComponent> PlayerIds;

			public EntityCommandBuffer.Concurrent CommandBuffer;

			[DeallocateOnJobCompletion] public NativeArray<Entity>                  DelayedEntities;
			[DeallocateOnJobCompletion] public NativeArray<DelayedPlayerConnection> DelayedData;

			public void Execute(Entity entity, int jobIndex, ref GamePlayer gamePlayer)
			{
				var count = DelayedEntities.Length;
				for (var ent = 0; ent != count; ent++)
				{
					if (DelayedData[ent].ServerId == gamePlayer.ServerId)
					{
						if (!PlayerReadyTag.Exists(entity))
						{
							CommandBuffer.AddComponent(jobIndex, entity, default(GamePlayerReadyTag));

							// Create connect event
							var evEnt = CommandBuffer.CreateEntity(jobIndex);
							CommandBuffer.AddComponent(jobIndex, evEnt, new PlayerConnectedEvent {Player = entity, ServerId = gamePlayer.ServerId});
						}
						else
						{
							Debug.LogWarning($"{entity} already had a 'GamePlayerReadyTag'");
						}

						// this is our player
						if (PlayerIds.Length > 0 && PlayerIds[0].Value == gamePlayer.ServerId)
						{
							CommandBuffer.AddComponent(jobIndex, entity, default(GamePlayerLocalTag));
						}

						CommandBuffer.DestroyEntity(jobIndex, DelayedEntities[ent]);
					}
				}
			}
		}

		private BeginSimulationEntityCommandBufferSystem m_Barrier;
		private EntityQuery                              m_DelayedQuery;
		private EntityQuery                              m_PreviousEventQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier            = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
			m_DelayedQuery       = GetEntityQuery(typeof(DelayedPlayerConnection));
			m_PreviousEventQuery = GetEntityQuery(typeof(PlayerConnectedEvent));
		}

		protected override unsafe JobHandle OnUpdate(JobHandle inputDeps)
		{
			var peLength = m_PreviousEventQuery.CalculateLength();
			if (peLength > 0)
			{
				EntityManager.DestroyEntity(m_PreviousEventQuery);
			}

			var playerIds = new NativeArray<NetworkIdComponent>(1, Allocator.TempJob);
			inputDeps = new FindFirstNetworkIdJob
			{
				PlayerIds = playerIds
			}.Schedule(this, inputDeps);
			
			m_DelayedQuery.AddDependency(inputDeps);
			var findPlayerJob = new FindPlayerJob
			{
				PlayerReadyTag = GetComponentDataFromEntity<GamePlayerReadyTag>(),
				CommandBuffer  = m_Barrier.CreateCommandBuffer().ToConcurrent(),

				DelayedEntities = m_DelayedQuery.ToEntityArray(Allocator.TempJob, out var dep1),
				DelayedData     = m_DelayedQuery.ToComponentDataArray<DelayedPlayerConnection>(Allocator.TempJob, out var dep2),
				PlayerIds       = playerIds
			};
			
			inputDeps = findPlayerJob.Schedule(this, JobHandle.CombineDependencies(inputDeps, dep1, dep2));

			m_Barrier.AddJobHandleForProducer(inputDeps);
			m_DelayedQuery.CompleteDependency();
			
			//inputDeps.Complete();

			return inputDeps;
		}
	}
}