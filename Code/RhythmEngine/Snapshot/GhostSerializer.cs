using package.patapon.core;
using StormiumTeam.GameBase;
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

		public ComponentType                                        ComponentTypeOwner;
		public ArchetypeChunkComponentType<Owner>                   GhostOwnerType;
		public ComponentType                                        ComponentTypeSettings;
		public ArchetypeChunkComponentType<RhythmEngineSettings>    GhostSettingsType;
		public ComponentType                                        ComponentTypeProcess;
		public ArchetypeChunkComponentType<FlowRhythmEngineProcess> GhostProcessType;
		public ComponentType                                        ComponentTypeState;
		public ArchetypeChunkComponentType<RhythmEngineState>       GhostStateType;

		public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;
		
		public void BeginSerialize(ComponentSystemBase system)
		{
			ComponentTypeOwner    = ComponentType.ReadWrite<Owner>();
			GhostOwnerType        = system.GetArchetypeChunkComponentType<Owner>();
			ComponentTypeSettings = ComponentType.ReadWrite<RhythmEngineSettings>();
			GhostSettingsType     = system.GetArchetypeChunkComponentType<RhythmEngineSettings>();
			ComponentTypeProcess  = ComponentType.ReadWrite<FlowRhythmEngineProcess>();
			GhostProcessType      = system.GetArchetypeChunkComponentType<FlowRhythmEngineProcess>();
			ComponentTypeState    = ComponentType.ReadWrite<RhythmEngineState>();
			GhostStateType        = system.GetArchetypeChunkComponentType<RhythmEngineState>();

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
				if (types[i] == ComponentTypeProcess) matches++;
				if (types[i] == ComponentTypeState) matches++;
			}

			return matches == 4;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref RhythmEngineSnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var owner = chunk.GetNativeArray(GhostOwnerType)[ent];
			snapshot.OwnerGhostId = GhostStateFromEntity.Exists(owner.Target) ? GhostStateFromEntity[owner.Target].ghostId : 0;

			var settings = chunk.GetNativeArray(GhostSettingsType)[ent];
			snapshot.MaxBeats = settings.MaxBeats;
			snapshot.UseClientSimulation = settings.UseClientSimulation;
			snapshot.BeatInterval = settings.BeatInterval;

			var process = chunk.GetNativeArray(GhostProcessType)[ent];
			snapshot.Beat      = process.Beat;
			snapshot.StartTime = process.StartTime;

			var state = chunk.GetNativeArray(GhostStateType)[ent];
			snapshot.IsPaused = state.IsPaused;
		}
	}
}