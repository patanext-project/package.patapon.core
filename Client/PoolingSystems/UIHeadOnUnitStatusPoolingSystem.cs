using DefaultNamespace;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.GameModes.Interface;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon.Client.PoolingSystems
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