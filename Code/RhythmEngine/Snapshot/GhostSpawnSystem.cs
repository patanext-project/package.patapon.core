using package.patapon.core;
using package.patapon.def.Data;
using Runtime.Systems;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

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

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new SyncJob()
			{
				SnapshotFromEntity = GetBufferFromEntity<DefaultRhythmEngineSnapshotData>(),
				TargetTick         = NetworkTimeSystem.predictTargetTick
			}.Schedule(this, inputDeps);
		}
	}
}