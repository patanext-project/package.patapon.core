using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

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

		public ComponentType ComponentTypeOwner;
		public ArchetypeChunkComponentType<Owner> GhostOwnerType;
		public ComponentType                                            ComponentTypeSettings;
		public ArchetypeChunkComponentType<DefaultRhythmEngineSettings> GhostSettingsType;
		public ComponentType                                            ComponentTypeState;
		public ArchetypeChunkComponentType<DefaultRhythmEngineState>    GhostStateType;

		public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

		public void BeginSerialize(ComponentSystemBase system)
		{
			ComponentTypeOwner    = ComponentType.ReadWrite<Owner>();
			GhostOwnerType        = system.GetArchetypeChunkComponentType<Owner>();
			ComponentTypeSettings = ComponentType.ReadWrite<DefaultRhythmEngineSettings>();
			GhostSettingsType     = system.GetArchetypeChunkComponentType<DefaultRhythmEngineSettings>();
			ComponentTypeState    = ComponentType.ReadWrite<DefaultRhythmEngineState>();
			GhostStateType        = system.GetArchetypeChunkComponentType<DefaultRhythmEngineState>();

			GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();
		}

		public bool CanSerialize(EntityArchetype arch)
		{
			var matches = 0;
			var types   = arch.GetComponentTypes();
			for (var i = 0; i != types.Length; i++)
			{
				if (types[i] == ComponentTypeOwner) matches++;
				if (types[i] == ComponentTypeSettings) matches++;
				if (types[i] == ComponentTypeState) matches++;
			}

			return matches == 3;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref DefaultRhythmEngineSnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var owner = chunk.GetNativeArray(GhostOwnerType)[ent];
			snapshot.OwnerGhostId = GhostStateFromEntity.Exists(owner.Target) ? GhostStateFromEntity[owner.Target].ghostId : 0;

			var settings = chunk.GetNativeArray(GhostSettingsType)[ent];
			snapshot.MaxBeats = settings.MaxBeats;

			var state = chunk.GetNativeArray(GhostStateType)[ent];
			snapshot.Beat     = state.Beat;
			snapshot.IsPaused = state.IsPaused;
		}
	}
}