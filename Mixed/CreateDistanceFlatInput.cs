using Unity.Mathematics;
using Unity.Physics;

namespace Patapon.Mixed.Utilities
{
	public static unsafe class CreateDistanceFlatInput
	{
		public static ColliderDistanceInput ColliderWithOffset(Collider* collider, float2 unitXY, float2 offset, float maxDistance = 0)
		{
			return new ColliderDistanceInput
			{
				Collider    = collider,
				MaxDistance = maxDistance,
				Transform   = new RigidTransform(quaternion.identity, new float3(unitXY + offset, 0))
			};
		}
	}
}