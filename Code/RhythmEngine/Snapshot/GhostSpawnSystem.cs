using package.patapon.core;
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
				ComponentType.ReadWrite<DefaultRhythmEngineSnapshotData>(),
				ComponentType.ReadWrite<DefaultRhythmEngineSettings>(),
				ComponentType.ReadWrite<DefaultRhythmEngineState>(),
				ComponentType.ReadWrite<FlowRhythmEngineSettingsData>(),
				ComponentType.ReadWrite<FlowRhythmEngineProcessData>()
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return default;
		}
	}
}