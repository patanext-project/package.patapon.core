using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Revolution.NetCode;
using UnityEngine;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	public class RhythmEngineClientSimulateLocalSystem : JobGameBaseSystem
	{
		[RequireComponentTag(typeof(RhythmEngineSimulateTag))]
		[BurstCompile]
		private struct SimulateJob : IJobForEachWithEntity<RhythmEngineSettings, RhythmEngineState, RhythmEngineProcess, RhythmPredictedProcess>
		{
			public UTick ServerTick;

			[BurstDiscard]
			private void NonBurst_ThrowWarning0(Entity entity)
			{
				Debug.LogWarning($"Engine '{entity}' had a FlowRhythmEngineSettingsData.BeatInterval of 0 (or less), this is not accepted.");
			}

			[BurstDiscard]
			private void NonBurst_ThrowWarning1(Entity entity, int beatDiff)
			{
				Debug.LogWarning($"Engine '{entity}' had a large different between the simulated and predicted process (diff={beatDiff})");
			}

			public void Execute(Entity entity, int index, ref RhythmEngineSettings settings, ref RhythmEngineState state, ref RhythmEngineProcess process, ref RhythmPredictedProcess predictedProcess)
			{
				if (state.IsPaused)
					return;

				var previousBeat = process.GetActivationBeat(settings.BeatInterval);

				process.Milliseconds = (int)(ServerTick.Ms - process.StartTime);
				if (settings.BeatInterval <= 0.0001f)
				{
					NonBurst_ThrowWarning0(entity);
					return;
				}

				var diff = process.GetActivationBeat(settings.BeatInterval) - previousBeat;
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
					NonBurst_ThrowWarning1(entity, diff);
				}
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (IsServer)
				return inputDeps;

			return new SimulateJob
			{
				ServerTick = World.GetExistingSystem<NetworkTimeSystem>().GetTickInterpolated()
			}.Schedule(this, inputDeps);
		}
	}
}