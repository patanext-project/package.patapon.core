using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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

	[UpdateInGroup(typeof(GhostSpawnSystemGroup))]
	[UpdateAfter(typeof(GamePlayerGhostSpawnSystem))]
	public class PlayerConnectedEventCreationSystem : JobComponentSystem
	{
		[RequireComponentTag(typeof(ReplicatedEntityComponent))]
		public struct FindJob : IJobForEachWithEntity<GamePlayer>
		{
			[ReadOnly]
			public ComponentDataFromEntity<GamePlayerReadyTag> PlayerReadyTag;

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

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var peLength = m_PreviousEventQuery.CalculateLength();
			if (peLength > 0)
			{
				EntityManager.DestroyEntity(m_PreviousEventQuery);
			}

			m_DelayedQuery.AddDependency(inputDeps);
			inputDeps = new FindJob
			{
				PlayerReadyTag = GetComponentDataFromEntity<GamePlayerReadyTag>(),
				CommandBuffer  = m_Barrier.CreateCommandBuffer().ToConcurrent(),

				DelayedEntities = m_DelayedQuery.ToEntityArray(Allocator.TempJob, out var dep1),
				DelayedData     = m_DelayedQuery.ToComponentDataArray<DelayedPlayerConnection>(Allocator.TempJob, out var dep2)
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, dep1, dep2));

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}