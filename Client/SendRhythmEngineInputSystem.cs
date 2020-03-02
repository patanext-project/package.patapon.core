using DefaultNamespace;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.RhythmEngine.Rpc;
using Patapon.Mixed.Systems;
using Revolution;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Systems.RhythmEngine
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UpdateAfter(typeof(ProcessRhythmEngineSystem))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class SendRhythmEngineInputSystem : AbsGameBaseSystem
	{
		private LazySystem<EndSimulationEntityCommandBufferSystem> m_EndBarrier;
		private LazySystem<GrabInputSystem>                        m_GrabInputSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			RequireSingletonForUpdate<IsClientWorldActive>();
		}

		protected override void OnUpdate()
		{
			Dependency.Complete();
			EntityManager.CompleteAllJobs();

			var targetKey = -1;

			var     grabInputSystem = m_GrabInputSystem.Get(World);
			ref var currentCommand  = ref grabInputSystem.LocalCommand;
			var     actions         = currentCommand.GetRhythmActions();
			for (var i = 0; i != actions.Length; i++)
				if (actions[i].FrameUpdate && actions[i].IsActive)
				{
					targetKey = i + 1;
					break;
				}

			if (targetKey < 0)
				return;

			var ecb = m_EndBarrier.Get(World).CreateCommandBuffer().ToConcurrent();

			Entities.WithAll<FlowSimulateProcess>().ForEach((Entity                                            entity, int                           nativeThreadIndex,
			                                                 ref RhythmEngineState                             state,  ref GamePredictedCommandState predictedCommand,
			                                                 ref DynamicBuffer<RhythmEngineCommandProgression> progression,
			                                                 in  RhythmEngineSettings                          settings, in FlowEngineProcess process,
			                                                 in  ReplicatedEntity                              replicatedEntity) =>
			{
				Entity                     rpcEnt;
				PressureEventFromClientRpc pressureEvent = default;

				if (state.IsPaused || process.GetFlowBeat(settings.BeatInterval) < 0)
				{
					//Debug.Log($"NOPE   paused? {state.IsPaused}, ms? {process.Milliseconds}");
					return;
				}

				var flowBeat = process.GetFlowBeat(settings.BeatInterval);

				pressureEvent.EngineGhostId = replicatedEntity.GhostId;
				pressureEvent.Key           = targetKey;
				pressureEvent.FlowBeat      = flowBeat;
				state.IsNewPressure         = true;

				var pressureData    = new FlowPressure(pressureEvent.Key, settings.BeatInterval, process.Milliseconds);
				var cmdChainEndFlow = FlowEngineProcess.CalculateFlowBeat(predictedCommand.State.ChainEndTime, settings.BeatInterval);
				var cmdEndFlow      = FlowEngineProcess.CalculateFlowBeat(predictedCommand.State.EndTime, settings.BeatInterval);
				// check for one beat space between inputs (should we just check for predicted commands? 'maybe' we would have a command with one beat space)
				var failFlag1 = progression.Length > 0 && pressureData.RenderBeat > progression[progression.Length - 1].Data.RenderBeat + 1
				                                       && cmdChainEndFlow > 0;
				// check if this is the first input and was started after the command input time
				var failFlag3 = pressureData.RenderBeat > cmdEndFlow
				                && progression.Length == 0
				                && cmdEndFlow > 0;
				// check for inputs that were done after the current command chain
				var failFlag2 = pressureData.RenderBeat >= cmdChainEndFlow
				                && cmdChainEndFlow > 0;
				failFlag2 = false; // this flag is deactivated for delayed reborn ability
				var failFlag0 = cmdEndFlow > flowBeat && cmdEndFlow > 0;

				if (state.IsRecovery(flowBeat))
				{
					predictedCommand.State.ChainEndTime = default;
				}
				else if (failFlag0 || failFlag1 || failFlag2 || failFlag3 || pressureData.GetAbsoluteScore() > FlowPressure.Error)
				{
					//Debug.Log($"{failFlag0} {failFlag1} {failFlag2} {failFlag3} (chainEnd={cmdChainEndFlow} end={cmdEndFlow} beat={flowBeat})");

					pressureEvent.ShouldStartRecovery   = true;
					state.NextBeatRecovery              = flowBeat + 1;
					predictedCommand.State.ChainEndTime = default;

					rpcEnt = ecb.CreateEntity(nativeThreadIndex);
					ecb.AddComponent(nativeThreadIndex, rpcEnt, new SendRpcCommandRequestComponent());
					ecb.AddComponent(nativeThreadIndex, rpcEnt, new RhythmRpcClientRecover
					{
						EngineGhostId = replicatedEntity.GhostId,
						ForceRecover  = true,
						RecoverBeat   = state.NextBeatRecovery
					});
				}
				else
				{
					progression.Add(new RhythmEngineCommandProgression
					{
						Data = pressureData
					});
				}

				pressureEvent.Score    = pressureData.Score;
				state.LastPressureBeat = math.max(state.LastPressureBeat, pressureData.RenderBeat);

				rpcEnt = ecb.CreateEntity(nativeThreadIndex);
				ecb.AddComponent(nativeThreadIndex, rpcEnt, new SendRpcCommandRequestComponent());
				ecb.AddComponent(nativeThreadIndex, rpcEnt, pressureEvent);

				var eventEnt = ecb.CreateEntity(nativeThreadIndex);
				ecb.AddComponent(nativeThreadIndex, eventEnt, new PressureEvent
				{
					Engine     = entity,
					Key        = pressureEvent.Key,
					TimeMs     = process.Milliseconds,
					RenderBeat = pressureData.RenderBeat,
					Score      = pressureData.Score
				});
			}).Schedule();

			m_EndBarrier.Value.AddJobHandleForProducer(Dependency);
		}
	}
}