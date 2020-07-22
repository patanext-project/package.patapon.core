using StormiumTeam.GameBase.Utility.AssetBackend;
using Unity.Mathematics;

namespace DataScripts.Models.Units.Projectiles
{
	public abstract class BaseProjectilePresentation : RuntimeAssetPresentation<BaseProjectilePresentation>
	{
	}

	public class DefaultProjectileBackend : RuntimeAssetBackend<BaseProjectilePresentation>
	{
		public float3     pos;
		public quaternion rot;

		public RigidTransform rt
		{
			get => new RigidTransform(rot, pos);
			set
			{
				pos = value.pos;
				rot = value.rot;
			}
		}
	}
}