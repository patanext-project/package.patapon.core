using package.patapon.core;
using Patapon4TLB.Default.Snapshot;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	public class RhythmEngineClientSimulateLocalSystem : JobGameBaseSystem
	{
		private struct SimulateJob : IJobForEachWithEntity<RhythmEngineSettings, RhythmEngineState, RhythmEngineProcess, RhythmPredictedProcess>
		{
			public uint CurrentTime;

			[BurstDiscard]
			private void NonBurst_ThrowWarning(Entity entity)
			{
				Debug.LogWarning($"Engine '{entity}' had a FlowRhythmEngineSettingsData.BeatInterval of 0 (or less), this is not accepted.");
			}

			public void Execute(Entity entity, int index, ref RhythmEngineSettings settings, ref RhythmEngineState state, ref RhythmEngineProcess process, ref RhythmPredictedProcess predictedProcess)
			{
				if (state.IsPaused)
					return;

				process.TimeTick = (int) (CurrentTime - process.StartTime);
				if (settings.BeatInterval <= 0.0001f)
				{
					NonBurst_ThrowWarning(entity);
					return;
				}

				var previousBeat = process.Beat;

				if (process.TimeTick != 0)
				{
					process.Beat = process.TimeTick / settings.BeatInterval;
					if (process.TimeTick < 0)
						process.Beat++;
				}
				else
				{
					process.Beat = 0;
				}

				var diff = predictedProcess.Beat - process.Beat;
				if (diff != 0)
				{
					state.IsNewBeat = true;
				}
				else
				{
					state.IsNewBeat = false;
				}

				if (diff > 2)
				{
					Debug.LogWarning("The difference of beats between the predicted process is greater!");
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (IsServer)
				return inputDeps;
			
			return new SimulateJob
			{
				CurrentTime = World.GetExistingSystem<SynchronizedSimulationTimeSystem>().Value.Predicted
			}.Schedule(this, inputDeps);
		}
	}
}