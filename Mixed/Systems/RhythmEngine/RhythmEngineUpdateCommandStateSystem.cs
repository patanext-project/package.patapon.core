using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Definitions;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.RhythmEngine.Rpc;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;

namespace Patapon.Mixed.Systems
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UpdateAfter(typeof(RhythmEngineCheckCommandValidity))]
	public class RhythmEngineUpdateCommandStateSystem : JobGameBaseSystem
	{
		private RpcQueue<RhythmRpcClientRecover> m_RpcQueue;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_RpcQueue = World.GetOrCreateSystem<RpcSystem>().GetRpcQueue<RhythmRpcClientRecover>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			Entity targetConnection                                  = default;
			if (HasSingleton<NetworkIdComponent>()) targetConnection = GetSingletonEntity<NetworkIdComponent>();

			var rpcQueue                    = m_RpcQueue;
			var isServer                    = IsServer;
			var simulateTagFromEntity       = GetComponentDataFromEntity<FlowSimulateProcess>(true);
			var predictedComboFromEntity    = GetComponentDataFromEntity<GameComboPredictedClient>();
			var predictedCommandFromEntity  = GetComponentDataFromEntity<GamePredictedCommandState>();
			var commandDefinitionFromEntity = GetComponentDataFromEntity<RhythmCommandDefinition>(true);
			var outgoingDataFromEntity      = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>();
			var replicatedDataFromEntity    = GetComponentDataFromEntity<ReplicatedEntity>(true);

			inputDeps =
				Entities
					.ForEach((Entity               entity,       ref RhythmEngineSettings settings, ref RhythmEngineState state, ref FlowEngineProcess process,
					          ref GameCommandState commandState, ref RhythmCurrentCommand rhythm,
					          ref GameComboState   comboState) =>
					{
						if (state.IsPaused
						    || !isServer && settings.UseClientSimulation && !simulateTagFromEntity.Exists(entity)
						    || process.Milliseconds < 0)
							return;

						var mercy = 1;
						if (isServer)
							mercy++; // we allow a mercy offset on a server in case the client is a bit laggy
						var cmdMercy = 0;
						if (isServer)
							cmdMercy = 3;

						var rhythmActiveAtFlowBeat = FlowEngineProcess.CalculateFlowBeat(rhythm.ActiveAtTime, settings.BeatInterval);
						var rhythmEndAtFlowBeat    = FlowEngineProcess.CalculateFlowBeat(rhythm.CustomEndTime, settings.BeatInterval);

						var checkStopBeat                                                    = math.max(state.LastPressureBeat + mercy, FlowEngineProcess.CalculateFlowBeat(commandState.EndTime, settings.BeatInterval) + cmdMercy);
						if (!isServer && simulateTagFromEntity.Exists(entity)) checkStopBeat = math.max(checkStopBeat, FlowEngineProcess.CalculateFlowBeat(predictedCommandFromEntity[entity].State.EndTime, settings.BeatInterval));

						var flowBeat       = process.GetFlowBeat(settings.BeatInterval);
						var activationBeat = process.GetActivationBeat(settings.BeatInterval);
						if (state.IsRecovery(flowBeat) || rhythmActiveAtFlowBeat < flowBeat && checkStopBeat < activationBeat
						                               || rhythm.CommandTarget == default && rhythm.HasPredictedCommands && rhythmActiveAtFlowBeat < state.LastPressureBeat
						                               || !rhythm.HasPredictedCommands && !isServer)
						{
							comboState.Chain        = 0;
							comboState.Score        = 0;
							comboState.IsFever      = false;
							comboState.JinnEnergy   = 0;
							comboState.ChainToFever = 0;

							commandState.ChainEndTime = -1;
							commandState.StartTime    = -1;
							commandState.EndTime      = -1;

							if (!isServer && simulateTagFromEntity.Exists(entity))
							{
								var p = predictedComboFromEntity[entity].State;

								predictedCommandFromEntity[entity] = new GamePredictedCommandState {State = commandState};
								predictedComboFromEntity[entity]   = new GameComboPredictedClient {State  = comboState};

								if (p.IsFever != comboState.IsFever
								    || p.Chain != comboState.Chain
								    || p.ChainToFever != comboState.ChainToFever)
									rpcQueue.Schedule(outgoingDataFromEntity[targetConnection], new RhythmRpcClientRecover
									{
										EngineGhostId = replicatedDataFromEntity[entity].GhostId,
										LooseChain    = true
									});
							}
						}

						if (rhythm.CommandTarget == default || state.IsRecovery(flowBeat))
						{
							commandState.StartTime    = -1;
							commandState.EndTime      = -1;
							commandState.ChainEndTime = -1;

							return;
						}

						var isActive   = false;
						var beatLength = 0;
						if (rhythm.CommandTarget != default)
						{
							var commandData = commandDefinitionFromEntity[rhythm.CommandTarget];
							beatLength = commandData.BeatLength;

							isActive =
								// check start
								(rhythm.ActiveAtTime < 0 || rhythmActiveAtFlowBeat <= flowBeat)
								// check end
								&& (rhythm.CustomEndTime == -2
								    || rhythmActiveAtFlowBeat >= 0 && rhythmActiveAtFlowBeat + commandData.BeatLength > flowBeat
								    || rhythmEndAtFlowBeat > flowBeat)
								// if both are set to no effect, then the command is not active
								&& rhythm.ActiveAtTime != 1 && rhythm.CustomEndTime != 1;
						}

						// prediction
						if (!isServer && settings.UseClientSimulation && simulateTagFromEntity.Exists(entity))
						{
							var previousPrediction = predictedCommandFromEntity[entity].State;
							var isNew              = state.ApplyCommandNextBeat;
							var madOp              = math.mad(beatLength, settings.BeatInterval, rhythm.ActiveAtTime);
							if (isNew)
							{
								previousPrediction.ChainEndTime = rhythm.CustomEndTime == 0 || rhythm.CustomEndTime == -1
									? (rhythmActiveAtFlowBeat + beatLength + 4) * settings.BeatInterval
									: rhythm.CustomEndTime;

								var predictedCombo = predictedComboFromEntity[entity];
								predictedCombo.State.Update(rhythm, true);

								predictedComboFromEntity[entity] = predictedCombo;

								previousPrediction.StartTime = rhythm.ActiveAtTime;
								previousPrediction.EndTime   = rhythm.CustomEndTime == 0 || rhythm.CustomEndTime == -1 ? madOp : rhythm.CustomEndTime;
							}

							predictedCommandFromEntity[entity] = new GamePredictedCommandState {State = previousPrediction};
						}
						else
						{
							var isNew = state.ApplyCommandNextBeat;
							var madOp = math.mad(beatLength, settings.BeatInterval, rhythm.ActiveAtTime);

							if (isNew)
							{
								commandState.StartTime = rhythm.ActiveAtTime;
								commandState.EndTime   = rhythm.CustomEndTime == -1 ? madOp : rhythm.CustomEndTime;
								commandState.ChainEndTime = rhythm.CustomEndTime == -1
									? (rhythmActiveAtFlowBeat + beatLength + 4) * settings.BeatInterval
									: rhythm.CustomEndTime;

								comboState.Update(rhythm, false);

								// add jinn energy
								if (comboState.Score >= 50) // we have a little bonus when doing a perfect command
									comboState.JinnEnergy += 10;
							}
						}

						state.ApplyCommandNextBeat = false;
					})
					.WithReadOnly(simulateTagFromEntity)
					.WithReadOnly(commandDefinitionFromEntity)
					.WithReadOnly(replicatedDataFromEntity)
					.WithNativeDisableParallelForRestriction(predictedComboFromEntity)
					.WithNativeDisableParallelForRestriction(predictedCommandFromEntity)
					.WithNativeDisableParallelForRestriction(outgoingDataFromEntity)
					.Schedule(inputDeps);

			return inputDeps;
		}
	}
}