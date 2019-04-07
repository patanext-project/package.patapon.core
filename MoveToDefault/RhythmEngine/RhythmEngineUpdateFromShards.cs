using JetBrains.Annotations;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	[UsedImplicitly]
	[DisableAutoCreation]
	public class RhythmEngineUpdateFromShards : GameBaseSystem
	{
		[BurstCompile]
		private struct Job : IJobProcessComponentDataWithEntity<DefaultRhythmEngineData.Predicted, FlowRhythmEngineProcessData,
			DefaultRhythmEngineData.Settings, FlowRhythmEngineSettingsData, FlowCommandManagerSettingsData>
		{
			public void Execute(Entity                                entity,    int                                         index,
			                    ref DefaultRhythmEngineData.Predicted predicted, [ReadOnly] ref FlowRhythmEngineProcessData  flowProcess,
			                    ref DefaultRhythmEngineData.Settings  settings,  [ReadOnly] ref FlowRhythmEngineSettingsData flowSettings, ref FlowCommandManagerSettingsData cmdSettings)
			{
				predicted.Beat    = flowProcess.Beat;
				settings.MaxBeats = cmdSettings.MaxBeats;
			}
		}

		protected override void OnUpdate()
		{
			SetDependency(new Job().Schedule(this, GetDependency()));
		}
	}
}