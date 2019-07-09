using package.patapon.core;
using package.patapon.def.Data;
using Runtime.Systems;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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
				ComponentType.ReadWrite<Owner>(),
				ComponentType.ReadWrite<RhythmEngineSnapshotData>(),
				ComponentType.ReadWrite<RhythmEngineState>(),
				ComponentType.ReadWrite<RhythmEngineCurrentCommand>(),
				ComponentType.ReadWrite<RhythmEngineProcess>(),
				ComponentType.ReadWrite<RhythmPredictedProcess>(),
				ComponentType.ReadWrite<ShardRhythmEngine>(),
				ComponentType.ReadWrite<FlowCommandManagerTypeDefinition>(),
				ComponentType.ReadWrite<GameCommandState>(),
				ComponentType.ReadWrite<GameComboState>(),
				ComponentType.ReadWrite<GameComboPredictedClient>(),
				ComponentType.ReadWrite<GamePredictedCommandState>(),
				ComponentType.ReadWrite<RhythmCurrentCommand>(),
				ComponentType.ReadWrite<ReplicatedEntityComponent>()
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return default;
		}
	}

	public struct RhythmPredictedProcess : IComponentData
	{
		public int    Beat;
		public double Time;
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(DefaultRhythmEngineGhostSpawnSystem))]
	[UpdateBefore(typeof(ConvertGhostToOwnerSystem))]
	public class RhythmEngineGhostSyncSnapshot : JobComponentSystem
	{
		[BurstCompile]
		private struct SyncJob : IJobForEachWithEntity<Owner, RhythmEngineState, RhythmEngineSettings, RhythmEngineProcess, GameCommandState, RhythmPredictedProcess>
		{
			[ReadOnly] public BufferFromEntity<RhythmEngineSnapshotData> SnapshotFromEntity;
			public            uint                                       TargetTick;

			public uint ServerTime;

			[ReadOnly] public ComponentDataFromEntity<RhythmEngineSimulateTag> SimulateTagFromEntity;
			[ReadOnly] public NativeHashMap<int, Entity>                       CommandIdToEntity;
			[ReadOnly] public NativeHashMap<int, Entity>                       GhostEntityMap;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<RhythmCurrentCommand>     RhythmCurrentCommand;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GameComboState>           ComboStateFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GameComboPredictedClient> PredictedComboFromEntity;

			public void Execute(Entity entity, int _,
			                    // components
			                    ref Owner owner, ref RhythmEngineState state, ref RhythmEngineSettings settings, ref RhythmEngineProcess process, ref GameCommandState commandState,
			                    // prediction
			                    ref RhythmPredictedProcess predictedProcess)
			{
				SnapshotFromEntity[entity].GetDataAtTick(TargetTick, out var snapshotData);

				GhostEntityMap.TryGetValue(snapshotData.OwnerGhostId, out owner.Target);

				settings.MaxBeats            = (int) snapshotData.MaxBeats;
				settings.BeatInterval        = (int) snapshotData.BeatInterval;
				settings.UseClientSimulation = snapshotData.UseClientSimulation;

				state.IsPaused         = snapshotData.IsPaused;
				state.NextBeatRecovery = math.max(state.NextBeatRecovery, snapshotData.Recovery);

				commandState.StartTime = snapshotData.CommandStartTime;
				commandState.EndTime   = snapshotData.CommandEndTime;
				commandState.ChainEndTime = snapshotData.CommandChainEndTime;

				process.StartTime = snapshotData.StartTime;
				if (!SimulateTagFromEntity.Exists(entity))
				{
					process.TimeTick = (int)(ServerTime - snapshotData.StartTime);

					CommandIdToEntity.TryGetValue(snapshotData.CommandTypeId, out var commandTarget);
					RhythmCurrentCommand[entity] = new RhythmCurrentCommand
					{
						ActiveAtTime  = snapshotData.CommandStartTime,
						CustomEndTime = snapshotData.CommandEndTime,
						CommandTarget = commandTarget
					};
				}

				predictedProcess.Time = snapshotData.StartTime > 0 ? (ServerTime - snapshotData.StartTime) * 0.001f : 0;

				var comboState     = ComboStateFromEntity[entity];
				var predictedCombo = PredictedComboFromEntity[entity];

				comboState = new GameComboState
				{
					Score         = snapshotData.ComboScore,
					Chain         = snapshotData.ComboChain,
					ChainToFever  = snapshotData.ComboChainToFever,
					IsFever       = snapshotData.ComboIsFever,
					JinnEnergy    = snapshotData.ComboJinnEnergy,
					JinnEnergyMax = snapshotData.ComboJinnEnergyMax
				};

				ComboStateFromEntity[entity]     = comboState;
				PredictedComboFromEntity[entity] = predictedCombo;
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
					CommandBuffer.AddComponent(jobIndex, entity, new RhythmEngineSimulateTag());
				}
				else
				{
					CommandBuffer.RemoveComponent(jobIndex, entity, typeof(RhythmEngineSimulateTag));
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
			var convertGhostMapSystem = World.GetExistingSystem<ConvertGhostEntityMap>();

			inputDeps = new SyncJob()
			{
				SnapshotFromEntity       = GetBufferFromEntity<RhythmEngineSnapshotData>(),
				ServerTime               = World.GetExistingSystem<SynchronizedSimulationTimeSystem>().Value.Predicted,
				SimulateTagFromEntity    = GetComponentDataFromEntity<RhythmEngineSimulateTag>(),
				ComboStateFromEntity     = GetComponentDataFromEntity<GameComboState>(),
				PredictedComboFromEntity = GetComponentDataFromEntity<GameComboPredictedClient>(),
				RhythmCurrentCommand     = GetComponentDataFromEntity<RhythmCurrentCommand>(),

				CommandIdToEntity = World.GetExistingSystem<RhythmCommandManager>().CommandIdToEntity,
				GhostEntityMap    = convertGhostMapSystem.HashMap,

				TargetTick = NetworkTimeSystem.predictTargetTick
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, convertGhostMapSystem.dependency));

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