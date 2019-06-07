using package.patapon.core;
using package.patapon.def.Data;
using Scripts.Utilities;
using Unity.Entities;
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
}