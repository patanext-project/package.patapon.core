using PataNext.Client.Core.Addressables;
using PataNext.Client.GameModes.VSHeadOn.Interface;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;

namespace PataNext.Client.PoolingSystems
{
	public class UIHeadOnUnitStatusPoolingSystem : PoolingSystem<UIHeadOnUnitStatusBackend, UIHeadOnUnitStatusPresentation>
	{
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Interface()
			              .GameMode("VSHeadOn")
			              .GetFile("VSHeadOn_UnitStatus.prefab");

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(UnitDescription), typeof(VersusHeadOnUnit));
		}
	}
}