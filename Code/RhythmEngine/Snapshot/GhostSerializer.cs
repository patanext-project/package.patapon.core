using package.patapon.core;
using StormiumTeam.GameBase;
using StormiumTeam.Networking.Utilities;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Default.Snapshot
{
	public struct DefaultRhythmEngineGhostSerializer : IGhostSerializer<RhythmEngineSnapshotData>
	{
		public unsafe int SnapshotSize => sizeof(RhythmEngineSnapshotData);

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			// actually, we don't really care if the rhythm engine data is sent every frame as players will simulate them client side.
			return 1;
		}

		public bool WantsPredictionDelta => false;

		public GhostComponentType<Owner>                   GhostOwnerType;
		public GhostComponentType<RhythmEngineSettings>    GhostSettingsType;
		public GhostComponentType<FlowRhythmEngineProcess> GhostProcessType;
		public GhostComponentType<RhythmEngineState>       GhostStateType;

		public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out GhostOwnerType);
			system.GetGhostComponentType(out GhostSettingsType);
			system.GetGhostComponentType(out GhostProcessType);
			system.GetGhostComponentType(out GhostStateType);

			GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();
		}

		public bool CanSerialize(EntityArchetype arch)
		{
			var matches = 0;
			var types   = arch.GetComponentTypes();
			for (var i = 0; i != types.Length; i++)
			{
				if (types[i] == GhostOwnerType) matches++;
				if (types[i] == GhostSettingsType) matches++;
				if (types[i] == GhostProcessType) matches++;
				if (types[i] == GhostStateType) matches++;
			}

			return matches == 4;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref RhythmEngineSnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var owner = chunk.GetNativeArray(GhostOwnerType.Archetype)[ent];
			snapshot.OwnerGhostId = GhostStateFromEntity.Exists(owner.Target) ? GhostStateFromEntity[owner.Target].ghostId : 0;

			var settings = chunk.GetNativeArray(GhostSettingsType.Archetype)[ent];
			snapshot.MaxBeats            = settings.MaxBeats;
			snapshot.UseClientSimulation = settings.UseClientSimulation;
			snapshot.BeatInterval        = settings.BeatInterval;

			var process = chunk.GetNativeArray(GhostProcessType.Archetype)[ent];
			snapshot.Beat      = process.Beat;
			snapshot.StartTime = process.StartTime;

			var state = chunk.GetNativeArray(GhostStateType.Archetype)[ent];
			snapshot.IsPaused = state.IsPaused;
		}
	}
}