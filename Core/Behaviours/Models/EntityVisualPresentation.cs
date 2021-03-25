using StormiumTeam.GameBase.Utility.AssetBackend;
using UnityEngine.Rendering;

namespace PataNext.Client.DataScripts.Models.Projectiles
{
	public class EntityVisualPresentation : RuntimeAssetPresentation
	{
		public override void OnBackendSet()
		{
			base.OnBackendSet();
			
			Backend.GetComponent<SortingGroup>()
			       .sortingLayerName = "MovableStructures";
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