using JetBrains.Annotations;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Patapon4TLB.Default
{
	[UsedImplicitly]
	[DisableAutoCreation]
	public class RhythmEngineProcessEvents : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Entity e, ref PressureEvent pressureEvent) =>
			{
				var processData  = EntityManager.GetComponentData<FlowRhythmEngineProcessData>(pressureEvent.Engine);
				var settingsData = EntityManager.GetComponentData<FlowRhythmEngineSettingsData>(pressureEvent.Engine);
				var cmdBuffer = EntityManager.GetBuffer<DefaultRhythmEngineCurrentCommand>(pressureEvent.Engine);

				cmdBuffer.Add(new DefaultRhythmEngineCurrentCommand
				{
					Data = new FlowRhythmPressureData(pressureEvent.Key, settingsData, processData)
				});
				
				PostUpdateCommands.DestroyEntity(e);
			});
		}
	}
}