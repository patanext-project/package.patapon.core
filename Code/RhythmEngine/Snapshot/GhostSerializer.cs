using package.patapon.core;
using StormiumTeam.GameBase;
using StormiumTeam.Networking.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Revolution.NetCode;

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

		public GhostComponentType<Owner>                GhostOwnerType;
		public GhostComponentType<RhythmEngineSettings> GhostEngineSettingsType;
		public GhostComponentType<RhythmEngineProcess>  GhostEngineProcessType;
		public GhostComponentType<RhythmEngineState>    GhostEngineStateType;
		public GhostComponentType<RhythmCurrentCommand> GhostCurrentCommandType;
		public GhostComponentType<GameCommandState>     GhostCommandStateType;
		public GhostComponentType<GameComboState>       GhostComboStateType;

		[NativeDisableContainerSafetyRestriction]
		public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

		[NativeDisableContainerSafetyRestriction]
		public ComponentDataFromEntity<RhythmCommandId> CommandDataFromEntity;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out GhostOwnerType);
			system.GetGhostComponentType(out GhostEngineSettingsType);
			system.GetGhostComponentType(out GhostEngineProcessType);
			system.GetGhostComponentType(out GhostEngineStateType);
			system.GetGhostComponentType(out GhostCurrentCommandType);
			system.GetGhostComponentType(out GhostCommandStateType);
			system.GetGhostComponentType(out GhostComboStateType);

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
				if (types[i] == GhostComboStateType) matches++;
			}

			return matches == 7;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref RhythmEngineSnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var owner = chunk.GetNativeArray(GhostOwnerType.Archetype)[ent];
			snapshot.OwnerGhostId = GhostStateFromEntity.Exists(owner.Target) ? GhostStateFromEntity[owner.Target].ghostId : 0;

			var engineSettings = chunk.GetNativeArray(GhostEngineSettingsType.Archetype)[ent];
			snapshot.UseClientSimulation = engineSettings.UseClientSimulation;
			snapshot.MaxBeats            = (uint) engineSettings.MaxBeats;
			snapshot.BeatInterval        = (uint) engineSettings.BeatInterval;

			var engineProcess = chunk.GetNativeArray(GhostEngineProcessType.Archetype)[ent];
			snapshot.StartTime = engineProcess.StartTime;

			var engineState = chunk.GetNativeArray(GhostEngineStateType.Archetype)[ent];
			snapshot.IsPaused = engineState.IsPaused;
			snapshot.Recovery = engineState.NextBeatRecovery;

			var currentCommand = chunk.GetNativeArray(GhostCurrentCommandType.Archetype)[ent];
			var commandState   = chunk.GetNativeArray(GhostCommandStateType.Archetype)[ent];
			snapshot.CommandTypeId    = currentCommand.CommandTarget == default ? 0 : CommandDataFromEntity[currentCommand.CommandTarget].Value;
			snapshot.CommandStartTime = commandState.StartTime;
			snapshot.CommandEndTime   = commandState.EndTime;
			snapshot.CommandChainEndTime = commandState.ChainEndTime;

			var comboState = chunk.GetNativeArray(GhostComboStateType.Archetype)[ent];
			snapshot.ComboIsFever       = comboState.IsFever;
			snapshot.ComboScore         = comboState.Score;
			snapshot.ComboChain         = (uint) comboState.Chain;
			snapshot.ComboChainToFever  = (uint) comboState.ChainToFever;
			snapshot.ComboJinnEnergy    = (uint) comboState.JinnEnergy;
			snapshot.ComboJinnEnergyMax = (uint) comboState.JinnEnergyMax;
		}
	}
}