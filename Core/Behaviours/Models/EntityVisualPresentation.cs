using StormiumTeam.GameBase.Utility.AssetBackend;
using UnityEngine.Rendering;

namespace PataNext.Client.DataScripts.Models.Projectiles
{
	public class EntityVisualPresentation : RuntimeAssetPresentation
	{
		public SortingGroup GetSortingGroup()
		{
			if (!TryGetComponent(out SortingGroup sortingGroup))
				sortingGroup = gameObject.AddComponent<SortingGroup>();

			return sortingGroup;
		}
	}

	public class EntityVisualBackend : RuntimeAssetBackend<EntityVisualPresentation>
	{
		public bool letPresentationUpdateTransform;
		public bool canBePooled;

		public override void OnReset()
		{
			base.OnReset();

			letPresentationUpdateTransform = false;
			canBePooled                    = true;
		}
	}
}