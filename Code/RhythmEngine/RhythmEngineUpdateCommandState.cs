using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
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
			public ComponentDataFromEntity<GamePredictedCommandState> PredictedFromEntity;

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

				if (rhythm.CommandTarget == default)
				{
					commandState.IsActive  = false;
					commandState.StartBeat = -1;
					commandState.EndBeat   = -1;
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
						(rhythm.ActiveAtBeat < 0 || rhythm.ActiveAtBeat <= process.Beat)
						// check end
						&& (rhythm.CustomEndBeat == -2
						    || (rhythm.ActiveAtBeat >= 0 && rhythm.ActiveAtBeat + commandData.BeatLength > process.Beat)
						    || rhythm.CustomEndBeat > process.Beat)
						// if both are set to no effect, then the command is not active
						&& rhythm.ActiveAtBeat != 1 && rhythm.CustomEndBeat != 1;
				}

				// prediction
				if (!IsServer && settings.UseClientSimulation && SimulateTagFromEntity.Exists(entity))
				{
					PredictedFromEntity[entity] = new GamePredictedCommandState
					{
						IsActive  = isActive,
						StartBeat = rhythm.ActiveAtBeat,
						// todo: we shouldn't do that
						EndBeat = (rhythm.CustomEndBeat == 0 || rhythm.CustomEndBeat == -1) ? rhythm.ActiveAtBeat + beatLength : rhythm.CustomEndBeat
					};
				}
				else
				{
					var isNew = isActive && (commandState.StartBeat != rhythm.ActiveAtBeat || !commandState.IsActive);

					commandState.IsActive  = isActive;
					commandState.StartBeat = rhythm.ActiveAtBeat;
					commandState.EndBeat   = rhythm.CustomEndBeat == -1 ? rhythm.ActiveAtBeat + beatLength : rhythm.CustomEndBeat;

					if (isNew)
					{
						var p = rhythm.Power - 50;
						if (p > 0 && comboState.Score < 0)
							comboState.Score = 0;
						
						comboState.Chain++;
						comboState.Score += p;
						if (!comboState.IsFever)
						{
							comboState.ChainToFever++;
						}

						if (comboState.IsFever)
						{
							// add jinn energy
						}

						if (comboState.ChainToFever >= 9)
						{
							comboState.IsFever = true;
						}
					}
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				IsServer              = IsServer,
				CommandDataFromEntity = GetComponentDataFromEntity<RhythmCommandData>(true),
				PredictedFromEntity   = GetComponentDataFromEntity<GamePredictedCommandState>(),
				SimulateTagFromEntity = GetComponentDataFromEntity<RhythmEngineSimulateTag>(true),
			}.Schedule(this, inputDeps);

			return inputDeps;
		}
	}
}