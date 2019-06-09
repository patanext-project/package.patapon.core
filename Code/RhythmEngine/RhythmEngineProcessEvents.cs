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
		/*protected override void OnUpdate()
		{
			Entities.ForEach((Entity e, ref PressureEvent pressureEvent) =>
			{
				var processData  = EntityManager.GetComponentData<FlowRhythmEngineProcessData>(pressureEvent.Engine);
				var settingsData = EntityManager.GetComponentData<FlowRhythmEngineSettingsData>(pressureEvent.Engine);
				var cmdBuffer = EntityManager.GetBuffer<RhythmEngineCurrentCommand>(pressureEvent.Engine);

				cmdBuffer.Add(new RhythmEngineCurrentCommand
				{
					Data = new FlowRhythmPressureData(pressureEvent.Key, settingsData, processData)
				});
				
				PostUpdateCommands.DestroyEntity(e);
			});
		}*/
		
		private struct AddPressureToCommandListJob : IJobForEachWithEntity<PressureEvent>
		{
			public bool IsServer;
			
			[ReadOnly] public ComponentDataFromEntity<RhythmEngineSettings> SettingsFromEntity;
			[ReadOnly] public ComponentDataFromEntity<FlowRhythmEngineProcess> ProcessFromEntity;

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
						Data = new FlowRhythmPressureData(pressureEvent.Key, settings.BeatInterval, process.Time, process.Beat)
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