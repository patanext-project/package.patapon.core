using package.patapon.core;
using package.patapon.def.Data;
using Patapon4TLB.Core;
using Runtime.Systems;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Default.Snapshot
{
	public class DefaultRhythmEngineGhostSpawnSystem : DefaultGhostSpawnSystem<DefaultRhythmEngineSnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				ComponentType.ReadWrite<DefaultRhythmEngineSettings>(),
				ComponentType.ReadWrite<GhostOwner>(),
				ComponentType.ReadWrite<Owner>(),
				ComponentType.ReadWrite<DefaultRhythmEngineSnapshotData>(),
				ComponentType.ReadWrite<DefaultRhythmEngineState>(),
				ComponentType.ReadWrite<DefaultRhythmEngineCurrentCommand>(),
				ComponentType.ReadWrite<FlowRhythmEngineSettingsData>(),
				ComponentType.ReadWrite<FlowRhythmEngineProcessData>(),
				ComponentType.ReadWrite<ShardRhythmEngine>(),
				ComponentType.ReadWrite<FlowCommandManagerTypeDefinition>(),
				ComponentType.ReadWrite<FlowCommandManagerSettingsData>(),
				ComponentType.ReadWrite<FlowCurrentCommand>(),
				ComponentType.ReadWrite<ReplicatedEntityComponent>()
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return default;
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(DefaultRhythmEngineGhostSpawnSystem))]
	[UpdateBefore(typeof(ConvertGhostToOwnerSystem))]
	public class RhythmEngineGhostSyncSnapshot : JobComponentSystem
	{
		private struct SyncJob : IJobForEachWithEntity<GhostOwner, DefaultRhythmEngineState, DefaultRhythmEngineSettings>
		{
			[ReadOnly] public BufferFromEntity<DefaultRhythmEngineSnapshotData> SnapshotFromEntity;
			public            uint                                              TargetTick;

			public void Execute(Entity entity, int _, ref GhostOwner owner, ref DefaultRhythmEngineState state, ref DefaultRhythmEngineSettings settings)
			{
				SnapshotFromEntity[entity].GetDataAtTick(TargetTick, out var snapshotData);

				owner.GhostId = snapshotData.OwnerGhostId;
				settings.Set(snapshotData);
				state.Set(snapshotData);
			}
		}

		private struct AddSimulationTagJob : IJobForEachWithEntity<DefaultRhythmEngineState, Owner>
		{
			[DeallocateOnJobCompletion, ReadOnly]
			public NativeArray<Entity>                         LocalPlayerEntity;
			public EntityCommandBuffer.Concurrent CommandBuffer;

			public void Execute(Entity entity, int jobIndex, ref DefaultRhythmEngineState state, ref Owner owner)
			{
				if (owner.Target == LocalPlayerEntity[0])
				{
					CommandBuffer.AddComponent(jobIndex, entity, new FlowRhythmEngineSimulateTag());
				}
			}
		}

		private EndSimulationEntityCommandBufferSystem m_Barrier;
		private EntityQuery                            m_LocalPlayerQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_LocalPlayerQuery = GetEntityQuery(typeof(GamePlayer), typeof(GamePlayerReadyTag), typeof(GamePlayerLocalTag));
			m_Barrier          = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new SyncJob()
			{
				SnapshotFromEntity = GetBufferFromEntity<DefaultRhythmEngineSnapshotData>(),
				TargetTick         = NetworkTimeSystem.predictTargetTick
			}.Schedule(this, inputDeps);

			var localPlayerLength = m_LocalPlayerQuery.CalculateLength();
			if (localPlayerLength == 1)
			{
				m_LocalPlayerQuery.AddDependency(inputDeps);
				inputDeps = new AddSimulationTagJob
				{
					LocalPlayerEntity = m_LocalPlayerQuery.ToEntityArray(Allocator.TempJob, out var queryDep),
					CommandBuffer     = m_Barrier.CreateCommandBuffer().ToConcurrent()
				}.Schedule(this, JobHandle.CombineDependencies(inputDeps, queryDep));
			}
			else if (localPlayerLength > 1)
			{
				Debug.LogError("There is more than 1 local player!");
			}
			m_Barrier.AddJobHandleForProducer(inputDeps);
			
			return inputDeps;
		}
	}
}