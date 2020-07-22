using PataNext.Client.GameModes.VSHeadOn.Interface;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;

namespace PataNext.Client.PoolingSystems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
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