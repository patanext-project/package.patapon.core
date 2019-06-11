using JetBrains.Annotations;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UsedImplicitly]
	public class RhythmEngineProcessEvents : JobGameBaseSystem
	{
		private struct AddPressureToCommandListJob : IJobForEachWithEntity<PressureEvent>
		{
			public bool IsServer;
			
			[ReadOnly] public ComponentDataFromEntity<RhythmEngineSettings> SettingsFromEntity;
			[ReadOnly] public ComponentDataFromEntity<RhythmEngineProcess> ProcessFromEntity;

			public BufferFromEntity<RhythmEngineClientPredictedCommand> PredictedCommandFromEntity;
			public BufferFromEntity<RhythmEngineCurrentCommand> CurrentCommandFromEntity;
			
			public void Execute(Entity entity, int index, [ReadOnly] ref PressureEvent pressureEvent)
			{
				var settings = SettingsFromEntity[pressureEvent.Engine];
				var process = ProcessFromEntity[pressureEvent.Engine];

				if (IsServer && settings.UseClientSimulation)
				{
					PredictedCommandFromEntity[pressureEvent.Engine].Add(new RhythmEngineClientPredictedCommand
					{
						Data = new RhythmPressureData(pressureEvent.Key, settings.BeatInterval, process.Time, process.Beat)
					});
					return;
				}
			}
		}


		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return inputDeps;
		}
	}
}