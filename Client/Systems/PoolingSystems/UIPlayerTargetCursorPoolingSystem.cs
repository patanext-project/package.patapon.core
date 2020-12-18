using System;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Models.InGame.Multiplayer;
using PataNext.Client.OrderSystems;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using UnityEngine.Rendering;

namespace PataNext.Client.PoolingSystems
{
	public class UIPlayerTargetCursorPoolingSystem : PoolingSystem<UIPlayerTargetCursorBackend, UIPlayerTargetCursorPresentation>
	{
		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};

		protected override AssetPath AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("InGame")
			              .Folder("Multiplayer")
			              .GetAsset("MpTargetCursor");

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