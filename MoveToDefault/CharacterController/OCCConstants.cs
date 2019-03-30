namespace MoveToDefault.CharacterController
{
	public static class OCCConstants
	{
		// Max slope limit.
		public const float k_MaxSlopeLimit = 90.0f;

		// Max slope angle on which character can slide down automatically.
		public const float k_MaxSlopeSlideAngle = 90.0f;

		// Distance to test for ground when sliding down slopes.
		public const float k_SlideDownSlopeTestDistance = 1.0f;

		// Slight delay before we stop sliding down slopes. To handle cases where sliding test fails for a few frames.
		public const float k_StopSlideDownSlopeDelay = 0.5f;

		// Distance to push away from slopes when sliding down them.
		public const float k_PushAwayFromSlopeDistance = 0.001f;

		// Minimum distance to use when checking ahead for steep slopes, when checking if it's safe to do the step offset.
		public const float k_MinCheckSteepSlopeAheadDistance = 0.2f;

		// Min skin width.
		public const float k_MinSkinWidth = 0.0001f;

		// The maximum move iterations. Mainly used as a fail safe to prevent an infinite loop.
		public const int k_MaxMoveIterations = 20;

		// Stick to the ground if it is less than this distance from the character.
		public const float k_MaxStickToGroundDownDistance = 1.0f;

		// Min distance to test for the ground when sticking to the ground.
		public const float k_MinStickToGroundDownDistance = 0.01f;

		// Max colliders to use in the overlap methods.
		public const int k_MaxOverlapColliders = 10;

		// Offset to use when moving to a collision point, to try to prevent overlapping the colliders
		public const float k_CollisionOffset = 0.001f;

		// Distance to test beneath the character when doing the grounded test
		public const float k_GroundedTestDistance = 0.001f;

		// Minimum distance to move. This minimizes small penetrations and inaccurate casts (e.g. into the floor)
		public const float k_MinMoveDistance = 0.0001f;

		// Minimum sqr distance to move. This minimizes small penetrations and inaccurate casts (e.g. into the floor)
		public const float k_MinMoveSqrDistance = k_MinMoveDistance * k_MinMoveDistance;

		// Minimum step offset height to move (if character has a step offset).
		public const float k_MinStepOffsetHeight = k_MinMoveDistance;

		// Small value to test if the movement vector is small.
		public const float k_SmallMoveVector = 1e-6f;

		// If angle between raycast and capsule/sphere cast normal is less than this then use the raycast normal, which is more accurate.
		public const float k_MaxAngleToUseRaycastNormal = 5.0f;

		// Scale the capsule/sphere hit distance when doing the additional raycast to get a more accurate normal
		public const float k_RaycastScaleDistance = 2.0f;

		// Slope check ahead is clamped by the distance moved multiplied by this scale.
		public const float k_SlopeCheckDistanceMultiplier = 5.0f;
	}
}