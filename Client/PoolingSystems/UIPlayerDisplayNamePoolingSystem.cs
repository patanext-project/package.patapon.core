using System;
using GameBase.Roles.Components;
using GameBase.Roles.Descriptions;
using PataNext.Client.Graphics.Models.InGame.Multiplayer;
using PataNext.Client.OrderSystems;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine.Rendering;

namespace PataNext.Client.PoolingSystems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UIPlayerDisplayNamePoolingSystem : PoolingSystem<UIPlayerDisplayNameBackend, UIPlayerDisplayNamePresentation>
	{
		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};
		
		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Multiplayer")
			              .GetFile("MpDisplayName.prefab");

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(UnitDescription), typeof(Relative<PlayerDescription>));
		}
		
		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);
			
			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "OverlayUI";
			sortingGroup.sortingOrder = World.GetExistingSystem<UIPlayerDisplayNameOrderSystem>().Order;
		}
	}
}