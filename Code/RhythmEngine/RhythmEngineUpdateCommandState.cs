using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Burst;
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

			[NativeDisableParallelForRestriction]
			public NativeArray<bool> SendEvent;

			[NativeDisableParallelForRestriction]
			public NativeArray<RhythmRpcClientRecover> RecoverEvent;

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

				var rhythmActiveAtFlowBeat = RhythmEngineProcess.CalculateFlowBeat(rhythm.ActiveAtTime, settings.BeatInterval);
				var rhythmEndAtFlowBeat    = RhythmEngineProcess.CalculateFlowBeat(rhythm.CustomEndTime, settings.BeatInterval);

				var checkStopBeat = math.max(state.LastPressureBeat, RhythmEngineProcess.CalculateFlowBeat(commandState.EndTime, settings.BeatInterval) + 1);
				if (!IsServer && SimulateTagFromEntity.Exists(entity))
				{
					checkStopBeat = math.max(checkStopBeat, RhythmEngineProcess.CalculateFlowBeat(PredictedCommandFromEntity[entity].State.EndTime, settings.BeatInterval) + 1);
				}

				var flowBeat = process.GetFlowBeat(settings.BeatInterval);
				if (state.IsRecovery(flowBeat) || (!commandState.HasActivity(process.TimeTick, settings.BeatInterval) && rhythmActiveAtFlowBeat < flowBeat && checkStopBeat + mercy < flowBeat)
				                               || (rhythm.CommandTarget == default && rhythm.HasPredictedCommands && rhythmActiveAtFlowBeat < state.LastPressureBeat))
				{
					comboState.Chain        = 0;
					comboState.Score        = 0;
					comboState.IsFever      = false;
					comboState.JinnEnergy   = 0;
					comboState.ChainToFever = 0;

					commandState.ChainEndTime = -1;
					commandState.StartTime    = -1;
					commandState.EndTime      = -1;

					if (!IsServer && SimulateTagFromEntity.Exists(entity))
					{
						var p = PredictedComboFromEntity[entity].State;

						PredictedCommandFromEntity[entity] = new GamePredictedCommandState {State = commandState};
						PredictedComboFromEntity[entity]   = new GameComboPredictedClient {State  = comboState};
						
						if (p.IsFever != comboState.IsFever
						    || p.Chain != comboState.Chain
						    || p.ChainToFever != comboState.ChainToFever)
						{
							SendEvent[0]    = true;
							RecoverEvent[0] = new RhythmRpcClientRecover {LooseChain = true};
						}
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
					var commandData = CommandDataFromEntity[rhythm.CommandTarget];
					beatLength = commandData.BeatLength;

					isActive =
						// check start
						(rhythm.ActiveAtTime < 0 || rhythmActiveAtFlowBeat <= flowBeat)
						// check end
						&& (rhythm.CustomEndTime == -2
						    || (rhythmActiveAtFlowBeat >= 0 && rhythmActiveAtFlowBeat + commandData.BeatLength > flowBeat)
						    || rhythmEndAtFlowBeat > flowBeat)
						// if both are set to no effect, then the command is not active
						&& rhythm.ActiveAtTime != 1 && rhythm.CustomEndTime != 1;
				}

				// prediction
				if (!IsServer && settings.UseClientSimulation && SimulateTagFromEntity.Exists(entity))
				{
					var previousPrediction = PredictedCommandFromEntity[entity].State;
					var isNew              = state.ApplyCommandNextBeat;
					var madOp              = math.mad(beatLength, settings.BeatInterval, rhythm.ActiveAtTime);
					if (isNew)
					{
						previousPrediction.ChainEndTime = (rhythm.CustomEndTime == 0 || rhythm.CustomEndTime == -1)
							? (rhythmActiveAtFlowBeat + beatLength + 4) * settings.BeatInterval
							: rhythm.CustomEndTime;

						var predictedCombo = PredictedComboFromEntity[entity];
						predictedCombo.State.Update(rhythm, true);

						PredictedComboFromEntity[entity] = predictedCombo;

						previousPrediction.StartTime = rhythm.ActiveAtTime;
						previousPrediction.EndTime   = (rhythm.CustomEndTime == 0 || rhythm.CustomEndTime == -1) ? madOp : rhythm.CustomEndTime;
					}

					PredictedCommandFromEntity[entity] = new GamePredictedCommandState {State = previousPrediction};
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
					}
				}

				state.ApplyCommandNextBeat = false;
			}
		}

		[BurstCompile]
		private struct SendRpcRecoverEvent : IJobForEachWithEntity<NetworkIdComponent>
		{
			[DeallocateOnJobCompletion] public NativeArray<bool>                   SendEvent;
			[DeallocateOnJobCompletion] public NativeArray<RhythmRpcClientRecover> RecoverEvent;

			public RpcQueue<RhythmRpcClientRecover> RpcRecoverQueue;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingDataBufferFromEntity;

			public void Execute(Entity entity, int index, ref NetworkIdComponent networkIdComponent)
			{
				if (!SendEvent[0])
					return;

				RpcRecoverQueue.Schedule(OutgoingDataBufferFromEntity[entity], RecoverEvent[0]);
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var sendEventSingleArray = new NativeArray<bool>(1, Allocator.TempJob);
			var rpcEventSingleArray  = new NativeArray<RhythmRpcClientRecover>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

			inputDeps = new Job
			{
				IsServer                   = IsServer,
				
				SendEvent = sendEventSingleArray,
				RecoverEvent = rpcEventSingleArray,
				
				CommandDataFromEntity      = GetComponentDataFromEntity<RhythmCommandData>(true),
				PredictedCommandFromEntity = GetComponentDataFromEntity<GamePredictedCommandState>(),
				SimulateTagFromEntity      = GetComponentDataFromEntity<RhythmEngineSimulateTag>(true),
				PredictedComboFromEntity   = GetComponentDataFromEntity<GameComboPredictedClient>(),
			}.Schedule(this, inputDeps);

			if (!IsServer)
			{
				var rpcQueue = World.GetExistingSystem<RpcQueueSystem<RhythmRpcClientRecover>>().GetRpcQueue();

				inputDeps = new SendRpcRecoverEvent
				{
					SendEvent    = sendEventSingleArray,
					RecoverEvent = rpcEventSingleArray,

					RpcRecoverQueue = rpcQueue,

					OutgoingDataBufferFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>()
				}.Schedule(this, inputDeps);
			}

			return inputDeps;
		}
	}
}