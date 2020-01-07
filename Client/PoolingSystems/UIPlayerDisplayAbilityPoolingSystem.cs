using System;
using DefaultNamespace;
using package.patapon.core.Models.InGame.Multiplayer;
using Patapon.Client.OrderSystems;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.Rendering;

namespace Patapon.Client.PoolingSystems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UIPlayerDisplayAbilityPoolingSystem : PoolingSystem<UIPlayerDisplayAbilityBackend, UIPlayerDisplayAbilityPresentation>
	{
		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};
		
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Multiplayer")
			              .GetFile("MpDisplayAbility.prefab");

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(UnitDescription), typeof(Relative<PlayerDescription>));
		}
		
		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);
			
			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "OverlayUI";
			sortingGroup.sortingOrder     = World.GetExistingSystem<UIPlayerDisplayAbilityOrderSystem>().Order;
		}
	}
}