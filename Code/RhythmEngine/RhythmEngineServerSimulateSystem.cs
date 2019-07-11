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
	public class RhythmEngineServerSimulateSystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct SimulateJob : IJobForEachWithEntity<RhythmEngineProcess, RhythmEngineState, RhythmEngineSettings>
		{
			public uint CurrentTime;

			[ReadOnly]
			public int FrameCount;

			[NativeDisableParallelForRestriction]
			public NativeList<FlowRhythmBeatEventProvider.Create> CreateBeatEventList;

			public EntityCommandBuffer.Concurrent EntityCommandBuffer;

			[BurstDiscard]
			private void NonBurst_ThrowWarning(Entity entity)
			{
				Debug.LogWarning($"Engine '{entity}' had a FlowRhythmEngineSettingsData.BeatInterval of 0 (or less), this is not accepted.");
			}

			public void Execute(Entity entity, int index, ref RhythmEngineProcess process, ref RhythmEngineState state, [ReadOnly] ref RhythmEngineSettings settings)
			{
				var previousBeat = process.GetActivationBeat(settings.BeatInterval);

				process.TimeTick = (int) (CurrentTime - process.StartTime);
				if (settings.BeatInterval <= 0.0001f)
				{
					NonBurst_ThrowWarning(entity);
					return;
				}

				state.IsNewBeat = false;

				var beatDiff = math.abs(previousBeat - process.GetActivationBeat(settings.BeatInterval));
				if (beatDiff == 0)
					return;

				state.IsNewBeat = true;

				if (beatDiff > 1)
				{
					// what to do?
				}

				CreateBeatEventList.Add(new FlowRhythmBeatEventProvider.Create
				{
					Target     = entity,
					FrameCount = FrameCount,
					Beat       = process.GetActivationBeat(settings.BeatInterval)
				});
			}
		}

		private RhythmEngineEndBarrier           m_EndBarrier;
		private FlowRhythmBeatEventProvider      m_BeatEventProvider;
		private SynchronizedSimulationTimeSystem m_SynchronizedSimulationTimeSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EndBarrier                       = World.GetOrCreateSystem<RhythmEngineEndBarrier>();
			m_BeatEventProvider                = World.GetOrCreateSystem<FlowRhythmBeatEventProvider>();
			m_SynchronizedSimulationTimeSystem = World.GetOrCreateSystem<SynchronizedSimulationTimeSystem>();

		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (!IsServer)
				return inputDeps;

			inputDeps = new SimulateJob
			{
				CurrentTime         = m_SynchronizedSimulationTimeSystem.Value.Predicted,
				FrameCount          = (int) ServerSimulationSystemGroup.ServerTick,
				CreateBeatEventList = m_BeatEventProvider.GetEntityDelayedList(),
				EntityCommandBuffer = m_EndBarrier.CreateCommandBuffer().ToConcurrent()
			}.Schedule(this, inputDeps);

			m_BeatEventProvider.AddJobHandleForProducer(inputDeps);
			m_EndBarrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}