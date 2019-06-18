using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UpdateAfter(typeof(RhythmEngineServerSimulateSystem))]
	[UpdateAfter(typeof(RhythmEngineClientSimulateLocalSystem))]
	[UpdateAfter(typeof(RhythmEngineCheckCommandValidity))]
	public class RhythmEngineUpdateCommandState : JobGameBaseSystem
	{
		private struct Job : IJobForEachWithEntity<RhythmEngineSettings, RhythmEngineState, RhythmEngineProcess, GameCommandState, RhythmCurrentCommand, GameComboState>
		{
			public bool IsServer;

			[ReadOnly]
			public ComponentDataFromEntity<RhythmCommandData> CommandDataFromEntity;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<GamePredictedCommandState> PredictedCommandFromEntity;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<GameComboPredictedClient> PredictedComboFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<RhythmEngineSimulateTag> SimulateTagFromEntity;

			public void Execute(Entity entity, int index,
			                    // components
			                    ref RhythmEngineSettings settings,     ref RhythmEngineState    state, ref RhythmEngineProcess process,
			                    ref GameCommandState     commandState, ref RhythmCurrentCommand rhythm,
			                    ref GameComboState       comboState)
			{
				if (state.IsPaused
				    || (!IsServer && settings.UseClientSimulation && !SimulateTagFromEntity.Exists(entity)))
					return;

				var mercy = 1;
				if (IsServer)
					mercy++; // we allow a mercy offset on a server in case the client is a bit laggy

				var checkStopBeat = math.max(state.LastPressureBeat, commandState.EndBeat + 1);
				if (!IsServer && SimulateTagFromEntity.Exists(entity))
				{
					checkStopBeat = math.max(checkStopBeat, PredictedCommandFromEntity[entity].EndBeat + 1);
				}

				var flowBeat = process.GetFlowBeat(settings.BeatInterval);
				if (state.IsRecovery(flowBeat) || (!commandState.IsActive && rhythm.ActiveAtBeat < flowBeat && checkStopBeat + mercy < flowBeat)
				                                   || (rhythm.CommandTarget == default && rhythm.HasPredictedCommands && rhythm.ActiveAtBeat < state.LastPressureBeat))
				{
					comboState.Chain        = 0;
					comboState.Score        = 0;
					comboState.IsFever      = false;
					comboState.JinnEnergy   = 0;
					comboState.ChainToFever = 0;

					commandState.IsActive = false;
					commandState.ChainEndBeat = -1;

					if (!IsServer && SimulateTagFromEntity.Exists(entity))
					{
						var p = PredictedCommandFromEntity[entity];
						PredictedComboFromEntity[entity] = new GameComboPredictedClient {State = comboState};
					}
				}

				if (rhythm.CommandTarget == default || state.IsRecovery(flowBeat))
				{
					commandState.IsActive     = false;
					commandState.StartBeat    = -1;
					commandState.EndBeat      = -1;
					commandState.ChainEndBeat = -1;
					return;
				}

				var isActive   = false;
				var beatLength = 0;
				if (rhythm.CommandTarget != default)
				{
					var commandData = CommandDataFromEntity[rhythm.CommandTarget];
					beatLength = commandData.BeatLength;

					isActive =
						// check start
						(rhythm.ActiveAtBeat < 0 || rhythm.ActiveAtBeat <= flowBeat)
						// check end
						&& (rhythm.CustomEndBeat == -2
						    || (rhythm.ActiveAtBeat >= 0 && rhythm.ActiveAtBeat + commandData.BeatLength > flowBeat)
						    || rhythm.CustomEndBeat > flowBeat)
						// if both are set to no effect, then the command is not active
						&& rhythm.ActiveAtBeat != 1 && rhythm.CustomEndBeat != 1;
				}

				// prediction
				if (!IsServer && settings.UseClientSimulation && SimulateTagFromEntity.Exists(entity))
				{
					var previousPrediction = PredictedCommandFromEntity[entity];
					var isNew = state.ApplyCommandNextBeat;
					if (isNew)
					{
						Debug.Log($"Command start at {rhythm.ActiveAtBeat}, currFlowBeat: {flowBeat}, currBeat: {process.GetActivationBeat(settings.BeatInterval)}");
						
						previousPrediction.ChainEndBeat = (rhythm.CustomEndBeat == 0 || rhythm.CustomEndBeat == -1) ? rhythm.ActiveAtBeat + beatLength * 2 : rhythm.CustomEndBeat;

						var predictedCombo = PredictedComboFromEntity[entity];
						predictedCombo.State.Update(rhythm, true);

						PredictedComboFromEntity[entity] = predictedCombo;
					}

					previousPrediction.IsActive = isActive;
					previousPrediction.StartBeat = rhythm.ActiveAtBeat;
					previousPrediction.EndBeat = (rhythm.CustomEndBeat == 0 || rhythm.CustomEndBeat == -1) ? rhythm.ActiveAtBeat + beatLength : rhythm.CustomEndBeat;

					PredictedCommandFromEntity[entity] = previousPrediction;
				}
				else
				{
					var isNew = state.ApplyCommandNextBeat;

					commandState.IsActive     = isActive;
					commandState.StartBeat    = rhythm.ActiveAtBeat;
					commandState.EndBeat      = rhythm.CustomEndBeat == -1 ? rhythm.ActiveAtBeat + beatLength : rhythm.CustomEndBeat;

					if (isNew)
					{
						commandState.ChainEndBeat = rhythm.CustomEndBeat == -1 ? rhythm.ActiveAtBeat + beatLength * 2 : rhythm.CustomEndBeat;
						
						comboState.Update(rhythm, false);
					}
				}

				state.ApplyCommandNextBeat = false;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				IsServer              = IsServer,
				CommandDataFromEntity = GetComponentDataFromEntity<RhythmCommandData>(true),
				PredictedCommandFromEntity   = GetComponentDataFromEntity<GamePredictedCommandState>(),
				SimulateTagFromEntity = GetComponentDataFromEntity<RhythmEngineSimulateTag>(true),
				PredictedComboFromEntity = GetComponentDataFromEntity<GameComboPredictedClient>(),
			}.Schedule(this, inputDeps);

			return inputDeps;
		}
	}
}