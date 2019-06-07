using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.Default.Snapshot
{
	public struct DefaultRhythmEngineGhostSerializer : IGhostSerializer<DefaultRhythmEngineSnapshotData>
	{
		public unsafe int SnapshotSize => sizeof(DefaultRhythmEngineSnapshotData);

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			// actually, we don't really care if the rhythm engine data is sent every frame as players will simulate them client side.
			return 1;
		}

		public bool WantsPredictionDelta => false;

		public ComponentType                                            ComponentTypeSettings;
		public ArchetypeChunkComponentType<DefaultRhythmEngineSettings> GhostSettingsType;
		public ComponentType                                            ComponentTypeState;
		public ArchetypeChunkComponentType<DefaultRhythmEngineState>    GhostStateType;

		public void BeginSerialize(ComponentSystemBase system)
		{
			ComponentTypeSettings = ComponentType.ReadWrite<DefaultRhythmEngineSettings>();
			GhostSettingsType     = system.GetArchetypeChunkComponentType<DefaultRhythmEngineSettings>();
			ComponentTypeState    = ComponentType.ReadWrite<DefaultRhythmEngineState>();
			GhostStateType        = system.GetArchetypeChunkComponentType<DefaultRhythmEngineState>();
		}

		public bool CanSerialize(EntityArchetype arch)
		{
			var matches = 0;
			var types   = arch.GetComponentTypes();
			for (var i = 0; i != types.Length; i++)
			{
				if (types[i] == ComponentTypeSettings) matches++;
				if (types[i] == ComponentTypeState) matches++;
			}

			return matches == 2;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref DefaultRhythmEngineSnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var settings = chunk.GetNativeArray(GhostSettingsType)[ent];
			snapshot.MaxBeats = settings.MaxBeats;

			var state = chunk.GetNativeArray(GhostStateType)[ent];
			snapshot.Beat     = state.Beat;
			snapshot.IsPaused = state.IsPaused;
		}
	}
}