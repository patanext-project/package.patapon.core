using Unity.Mathematics;
using static Unity.Mathematics.math;
using static MoveToDefault.CharacterController.OCCConstants;

namespace MoveToDefault.CharacterController
{
	public static class OCCMath
	{
		public static float SqrMagnitudeFrom(float3 vectorA, float3 vectorB)
		{
			var diff = vectorA - vectorB;
			return lengthsq(diff);
		}

		// Is the movement vector almost zero (i.e. very small)?
		public static bool IsMoveVectorAlmostZero(float3 moveVector)
		{
			var aMv = abs(moveVector);

			return !(aMv.x > k_SmallMoveVector
			         || aMv.y > k_SmallMoveVector
			         || aMv.z > k_SmallMoveVector);
		}

		// Taken from Mathf, rewritten for burst support.
		public static bool Approximately(double a, double b)
		{
			return abs(b - a) < max(1E-06 * max(abs(a), abs(b)), DBL_MIN_NORMAL * 8f);
		}
	}
}