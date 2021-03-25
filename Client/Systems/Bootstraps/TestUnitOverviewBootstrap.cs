using PataNext.Client.DataScripts.Interface.Menu.__Barracks;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Bootstrapping;
using Unity.Entities;

namespace PataNext.Client.Systems.Bootstraps
{
	public class TestUnitOverviewBootstrap : BaseBootstrapSystem
	{
		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(TestUnitOverviewBootstrap)});
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			var unit = EntityManager.CreateEntity(
			);

			EntityManager.AddComponentData(EntityManager.CreateEntity(), new CurrentUnitOverview(unit));

			EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}
}