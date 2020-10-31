using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Mathematics;

namespace PataNext.Client.DataScripts.Models.Projectiles
{
	public abstract class BaseProjectilePresentation : RuntimeAssetPresentation<BaseProjectilePresentation>
	{
	}

	public class ProjectileBackend : RuntimeAssetBackend<BaseProjectilePresentation>
	{
		public bool letPresentationUpdateTransform;

		public override void OnReset()
		{
			base.OnReset();

			letPresentationUpdateTransform = false;
		}
	}
}