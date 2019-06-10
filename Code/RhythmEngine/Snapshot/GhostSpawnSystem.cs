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
	public class DefaultRhythmEngineGhostSpawnSystem : DefaultGhostSpawnSystem<RhythmEngineSnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				ComponentType.ReadWrite<RhythmEngineSettings>(),
				ComponentType.ReadWrite<GhostOwner>(),
				ComponentType.ReadWrite<Owner>(),
				ComponentType.ReadWrite<RhythmEngineSnapshotData>(),
				ComponentType.ReadWrite<RhythmEngineState>(),
				ComponentType.ReadWrite<RhythmEngineCurrentCommand>(),
				ComponentType.ReadWrite<FlowRhythmEngineProcess>(),
				ComponentType.ReadWrite<FlowRhythmEnginePredictedProcess>(),
				ComponentType.ReadWrite<ShardRhythmEngine>(),
				ComponentType.ReadWrite<FlowCommandManagerTypeDefinition>(),
				ComponentType.ReadWrite<FlowCurrentCommand>(),
				ComponentType.ReadWrite<ReplicatedEntityComponent>()
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return default;
		}
	}

	public struct FlowRhythmEnginePredictedProcess : IComponentData
	{
		public int Beat;
		public double Time;
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(DefaultRhythmEngineGhostSpawnSystem))]
	[UpdateBefore(typeof(ConvertGhostToOwnerSystem))]
	public class RhythmEngineGhostSyncSnapshot : JobComponentSystem
	{
		private struct SyncJob : IJobForEachWithEntity<GhostOwner, RhythmEngineState, RhythmEngineSettings, FlowRhythmEngineProcess, FlowRhythmEnginePredictedProcess>
		{
			[ReadOnly] public BufferFromEntity<RhythmEngineSnapshotData> SnapshotFromEntity;
			public            uint                                       TargetTick;

			public uint ServerTime;

			[ReadOnly] public ComponentDataFromEntity<FlowRhythmEngineSimulateTag> SimulateTagFromEntity;

			public void Execute(Entity entity, int _, ref GhostOwner owner, ref RhythmEngineState state, ref RhythmEngineSettings settings, ref FlowRhythmEngineProcess process, ref FlowRhythmEnginePredictedProcess predictedProcess)
			{
				SnapshotFromEntity[entity].GetDataAtTick(TargetTick, out var snapshotData);

				owner.GhostId                = snapshotData.OwnerGhostId;
				settings.MaxBeats            = snapshotData.MaxBeats;
				settings.BeatInterval        = snapshotData.BeatInterval;
				settings.UseClientSimulation = snapshotData.UseClientSimulation;

				state.IsPaused = snapshotData.IsPaused;

				process.StartTime = snapshotData.StartTime;
				if (!SimulateTagFromEntity.Exists(entity))
				{
					process.Beat = snapshotData.Beat;
					process.Time = snapshotData.StartTime > 0 ? (ServerTime - snapshotData.StartTime) * 0.001f : 0;
				}

				predictedProcess.Beat = snapshotData.Beat;
				predictedProcess.Time = snapshotData.StartTime > 0 ? (ServerTime - snapshotData.StartTime) * 0.001f : 0;
			}
		}

		private struct AddSimulationTagJob : IJobForEachWithEntity<RhythmEngineState, Owner>
		{
			[DeallocateOnJobCompletion, ReadOnly]
			public NativeArray<Entity> LocalPlayerEntity;

			public EntityCommandBuffer.Concurrent CommandBuffer;

			public void Execute(Entity entity, int jobIndex, ref RhythmEngineState state, ref Owner owner)
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
				SnapshotFromEntity = GetBufferFromEntity<RhythmEngineSnapshotData>(),
				ServerTime = World.GetExistingSystem<SynchronizedSimulationTimeSystem>().Value.Predicted,
				SimulateTagFromEntity = GetComponentDataFromEntity<FlowRhythmEngineSimulateTag>(),
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