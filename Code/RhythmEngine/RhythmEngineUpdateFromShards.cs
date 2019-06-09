using JetBrains.Annotations;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Default
{
	/*[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UsedImplicitly]
	public class RhythmEngineUpdateFromShards : JobGameBaseSystem
	{
		[BurstCompile]
		private struct Job : IJobProcessComponentDataWithEntity<RhythmEngineState, FlowRhythmEngineProcessData,
			RhythmEngineSettings, FlowRhythmEngineSettingsData, FlowCommandManagerSettingsData>
		{
			public void Execute(Entity                          entity,   int                                         index,
			                    ref RhythmEngineState    state,    [ReadOnly] ref FlowRhythmEngineProcessData  flowProcess,
			                    ref RhythmEngineSettings settings, [ReadOnly] ref FlowRhythmEngineSettingsData flowSettings, ref FlowCommandManagerSettingsData cmdSettings)
			{
				state.Beat        = flowProcess.Beat;
				settings.MaxBeats = cmdSettings.MaxBeats;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job().Schedule(this, inputDeps);
		}
	}*/
}