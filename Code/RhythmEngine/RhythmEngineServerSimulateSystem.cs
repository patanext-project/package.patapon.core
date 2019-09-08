using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Revolution.NetCode;
using UnityEngine;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	public class RhythmEngineServerSimulateSystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct SimulateJob : IJobForEachWithEntity<RhythmEngineProcess, RhythmEngineState, RhythmEngineSettings>
		{
			public UTick CurrentTick;
			
			[BurstDiscard]
			private void NonBurst_ThrowWarning(Entity entity)
			{
				Debug.LogWarning($"Engine '{entity}' had a FlowRhythmEngineSettingsData.BeatInterval of 0 (or less), this is not accepted.");
			}

			public void Execute(Entity entity, int index, ref RhythmEngineProcess process, ref RhythmEngineState state, [ReadOnly] ref RhythmEngineSettings settings)
			{
				var previousBeat = process.GetActivationBeat(settings.BeatInterval);

				process.Milliseconds = (int)(CurrentTick.Ms - process.StartTime);
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
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (!IsServer)
				return inputDeps;

			inputDeps = new SimulateJob
			{
				CurrentTick         = World.GetExistingSystem<ServerSimulationSystemGroup>().GetTick(),
			}.Schedule(this, inputDeps);
			
			return inputDeps;
		}
	}
}