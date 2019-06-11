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
		public GhostComponentType<RhythmEngineSettings>    GhostEngineSettingsType;
		public GhostComponentType<RhythmEngineProcess> GhostEngineProcessType;
		public GhostComponentType<RhythmEngineState>       GhostEngineStateType;
		public GhostComponentType<RhythmCurrentCommand>      GhostCurrentCommandType;
		public GhostComponentType<GameCommandState>        GhostCommandStateType;

		public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;
		public ComponentDataFromEntity<RhythmCommandId>             CommandDataFromEntity;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out GhostOwnerType);
			system.GetGhostComponentType(out GhostEngineSettingsType);
			system.GetGhostComponentType(out GhostEngineProcessType);
			system.GetGhostComponentType(out GhostEngineStateType);
			system.GetGhostComponentType(out GhostCurrentCommandType);
			system.GetGhostComponentType(out GhostCommandStateType);

			GhostStateFromEntity  = system.GetComponentDataFromEntity<GhostSystemStateComponent>();
			CommandDataFromEntity = system.GetComponentDataFromEntity<RhythmCommandId>();
		}

		public bool CanSerialize(EntityArchetype arch)
		{
			var matches = 0;
			var types   = arch.GetComponentTypes();
			for (var i = 0; i != types.Length; i++)
			{
				if (types[i] == GhostOwnerType) matches++;
				if (types[i] == GhostEngineSettingsType) matches++;
				if (types[i] == GhostEngineProcessType) matches++;
				if (types[i] == GhostEngineStateType) matches++;
				if (types[i] == GhostCurrentCommandType) matches++;
				if (types[i] == GhostCommandStateType) matches++;
			}

			return matches == 6;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref RhythmEngineSnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var owner = chunk.GetNativeArray(GhostOwnerType.Archetype)[ent];
			snapshot.OwnerGhostId = GhostStateFromEntity.Exists(owner.Target) ? GhostStateFromEntity[owner.Target].ghostId : 0;

			var engineSettings = chunk.GetNativeArray(GhostEngineSettingsType.Archetype)[ent];
			snapshot.MaxBeats            = engineSettings.MaxBeats;
			snapshot.UseClientSimulation = engineSettings.UseClientSimulation;
			snapshot.BeatInterval        = engineSettings.BeatInterval;

			var engineProcess = chunk.GetNativeArray(GhostEngineProcessType.Archetype)[ent];
			snapshot.Beat      = engineProcess.Beat;
			snapshot.StartTime = engineProcess.StartTime;

			var engineState = chunk.GetNativeArray(GhostEngineStateType.Archetype)[ent];
			snapshot.IsPaused = engineState.IsPaused;

			var currentCommand = chunk.GetNativeArray(GhostCurrentCommandType.Archetype)[ent];
			var commandState   = chunk.GetNativeArray(GhostCommandStateType.Archetype)[ent];
			snapshot.CommandTypeId    = currentCommand.CommandTarget == default || !commandState.IsActive ? 0 : CommandDataFromEntity[currentCommand.CommandTarget].Value;
			snapshot.CommandStartBeat = commandState.StartBeat;
			snapshot.CommandEndBeat   = commandState.EndBeat;
		}
	}
}