using System;
using GameBase.Roles.Components;
using GameBase.Roles.Descriptions;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Models.InGame.Multiplayer;
using PataNext.Client.OrderSystems;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine.Rendering;

namespace PataNext.Client.PoolingSystems
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UIPlayerTargetCursorPoolingSystem : PoolingSystem<UIPlayerTargetCursorBackend, UIPlayerTargetCursorPresentation>
	{
		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};

		protected override string AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Multiplayer")
			              .GetFile("MpTargetCursor.prefab");

		protected override EntityQuery GetQuery()
		{
			// only display if there is a relative player...
			return GetEntityQuery(typeof(UnitTargetDescription), typeof(Relative<PlayerDescription>));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "OverlayUI";
			sortingGroup.sortingOrder     = World.GetExistingSystem<UIPlayerTargetCursorOrderSystem>().Order;
		}
	}
}