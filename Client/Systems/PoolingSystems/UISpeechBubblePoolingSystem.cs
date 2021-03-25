using System;
using PataNext.Client.Core.Addressables;
using PataNext.Client.DataScripts.Interface.Bubble;
using PataNext.Client.OrderSystems;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace PataNext.Client.PoolingSystems
{
	public class UISpeechBubblePoolingSystem : PoolingSystem<UIBubbleBackend, UIBubblePresentation>
	{
		protected override Type[] AdditionalBackendComponents => new Type[] {typeof(SortingGroup)};

		protected override AssetPath AddressableAsset =>
			AddressBuilder.Client()
			              .Folder("Models")
			              .Folder("UIBubble")
			              .GetAsset("UISpeechBubble");

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(SpeechBubble), typeof(Translation));
		}

		protected override void SpawnBackend(Entity target)
		{
			base.SpawnBackend(target);

			var sortingGroup = LastBackend.GetComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "OverlayUI";
			sortingGroup.sortingOrder     = World.GetExistingSystem<UIPlayerTargetCursorOrderSystem>().Order + 1;
		}
	}
}