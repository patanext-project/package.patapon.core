using StormiumTeam.GameBase.Utility.AssetBackend;

namespace PataNext.Client.DataScripts.Models.Projectiles
{
	public abstract class BaseProjectilePresentation : RuntimeAssetPresentation
	{
	}

	public class ProjectileBackend : RuntimeAssetBackend<BaseProjectilePresentation>
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