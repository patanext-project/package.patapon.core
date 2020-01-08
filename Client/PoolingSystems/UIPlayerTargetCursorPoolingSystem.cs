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