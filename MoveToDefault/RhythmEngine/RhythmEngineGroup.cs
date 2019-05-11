using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Default
{
	public class RhythmEngineGroup : ComponentSystemGroup
	{
		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			var registerDefaultSequenceCommands = World.GetOrCreateSystem<RegisterDefaultSequenceCommands>();
			var processEvents = World.GetOrCreateSystem<RhythmEngineProcessEvents>();
			var checkCurrentCommandValidity     = World.GetOrCreateSystem<RhythmEngineCheckCurrentCommandValidity>();

			registerDefaultSequenceCommands.SystemGroup_CanHaveDependency(true);
			processEvents.SystemGroup_CanHaveDependency(true);
			checkCurrentCommandValidity.SystemGroup_CanHaveDependency(true);

			AddSystemToUpdateList(registerDefaultSequenceCommands);
			AddSystemToUpdateList(processEvents);
			AddSystemToUpdateList(checkCurrentCommandValidity);
		}

		protected override void OnUpdate()
		{
			var dependency = default(JobHandle);
			
			foreach (var componentSystemBase in m_systemsToUpdate)
			{
				var system = (GameBaseSystem) componentSystemBase;
				
				system.SetDependency(dependency);
				system.Update();
				dependency = system.GetDependency();
				
				dependency.Complete();
			}
			
			dependency.Complete();
		}
	}
}