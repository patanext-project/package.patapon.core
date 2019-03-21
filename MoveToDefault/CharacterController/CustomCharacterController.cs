using System;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;
using static Unity.Mathematics.math;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Patapon4TLB.Default
{
	public struct CustomCharacterController : IComponentData
	{
		public float StepOffset;
		public float MaxSlope;

		public float SkinWidth;

		public bool  IsLocalHuman;
		public bool  SlowAgainstWalls;
		public float MinSlowAgainstWallsAngle;

		internal BlobAssetReference<Collider> QuerySmallCapsule, QueryBigCapsule;
		internal BlobAssetReference<Collider> QuerySmallSphere,  QueryBigSphere;

		public struct CollisionInfo
		{

		}
	}

	public unsafe class CustomCharacterControllerSystem : JobComponentSystem
	{

		// Max slope limit.
		const float k_MaxSlopeLimit = 90.0f;

		// Max slope angle on which character can slide down automatically.
		const float k_MaxSlopeSlideAngle = 90.0f;

		// Distance to test for ground when sliding down slopes.
		const float k_SlideDownSlopeTestDistance = 1.0f;

		// Slight delay before we stop sliding down slopes. To handle cases where sliding test fails for a few frames.
		const float k_StopSlideDownSlopeDelay = 0.5f;

		// Distance to push away from slopes when sliding down them.
		const float k_PushAwayFromSlopeDistance = 0.001f;

		// Minimum distance to use when checking ahead for steep slopes, when checking if it's safe to do the step offset.
		const float k_MinCheckSteepSlopeAheadDistance = 0.2f;

		// Min skin width.
		const float k_MinSkinWidth = 0.0001f;

		// The maximum move iterations. Mainly used as a fail safe to prevent an infinite loop.
		const int k_MaxMoveIterations = 20;

		// Stick to the ground if it is less than this distance from the character.
		const float k_MaxStickToGroundDownDistance = 1.0f;

		// Min distance to test for the ground when sticking to the ground.
		const float k_MinStickToGroundDownDistance = 0.01f;

		// Max colliders to use in the overlap methods.
		const int k_MaxOverlapColliders = 10;

		// Offset to use when moving to a collision point, to try to prevent overlapping the colliders
		const float k_CollisionOffset = 0.001f;

		// Distance to test beneath the character when doing the grounded test
		const float k_GroundedTestDistance = 0.001f;

		// Minimum distance to move. This minimizes small penetrations and inaccurate casts (e.g. into the floor)
		const float k_MinMoveDistance = 0.0001f;

		// Minimum sqr distance to move. This minimizes small penetrations and inaccurate casts (e.g. into the floor)
		const float k_MinMoveSqrDistance = k_MinMoveDistance * k_MinMoveDistance;

		// Minimum step offset height to move (if character has a step offset).
		const float k_MinStepOffsetHeight = k_MinMoveDistance;

		// Small value to test if the movement vector is small.
		const float k_SmallMoveVector = 1e-6f;

		// If angle between raycast and capsule/sphere cast normal is less than this then use the raycast normal, which is more accurate.
		const float k_MaxAngleToUseRaycastNormal = 5.0f;

		// Scale the capsule/sphere hit distance when doing the additional raycast to get a more accurate normal
		const float k_RaycastScaleDistance = 2.0f;

		// Slope check ahead is clamped by the distance moved multiplied by this scale.
		const float k_SlopeCheckDistanceMultiplier = 5.0f;

		private struct Job : IJobProcessComponentDataWithEntity<CustomCharacterController, Translation, Rotation, Velocity, PhysicsCollider>
		{
			public float dt;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith> CollideWithBufferArray;

			[ReadOnly]
			public PhysicsWorld PhysicsWorld;

			public void Execute(Entity e, int _, ref CustomCharacterController cc, ref Translation t, ref Rotation r, ref Velocity v, ref PhysicsCollider coll)
			{
				var cwBuffer     = CollideWithBufferArray[e];
				var prevPosition = t.Value;

				// Create a copy of capsule collider
				var colliderPtr = coll.ColliderPtr;

				var copiedColliderMemory      = stackalloc byte[colliderPtr->MemorySize];
				var capsuleColliderForQueries = (Unity.Physics.Collider*) copiedColliderMemory;
				UnsafeUtility.MemCpy(capsuleColliderForQueries, colliderPtr, colliderPtr->MemorySize);
				capsuleColliderForQueries->Filter = CollisionFilter.Default;

				// Tau and damping for character solver
				const float tau     = 0.4f;
				const float damping = 0.9f;

				// Check support
				var transform = new RigidTransform
				{
					pos = t.Value,
					rot = r.Value
				};

				var constraints  = new NativeArray<SurfaceConstraintInfo>(128 * 2, Allocator.Temp);
				var distanceHits = new NativeArray<DistanceHit>(128, Allocator.Temp);
				var castHits     = new NativeArray<ColliderCastHit>(128, Allocator.Temp);
				var prevVelocity = v.Value;

				CharacterControllerUtilities.CheckSupport(cwBuffer, PhysicsWorld, dt, transform, -math.up(), 1.5f,
					0.1f, capsuleColliderForQueries, ref constraints, ref distanceHits, out var supportState);

				CharacterControllerUtilities.CollideAndIntegrate(cwBuffer, PhysicsWorld, dt, 32, math.up(), new float3(0, -10, 0),
					1.0f, tau, damping, capsuleColliderForQueries,
					ref distanceHits, ref castHits, ref constraints,
					ref transform, ref v.Value);

				// if we are grounded, try to make a stair check
				var groundCastBefore = new ColliderCastInput
				{
					Collider    = capsuleColliderForQueries,
					Direction   = new float3(0, k_GroundedTestDistance, 0),
					Orientation = transform.rot,
					Position    = prevPosition
				};

				var groundCastAfter = new ColliderCastInput
				{
					Collider    = capsuleColliderForQueries,
					Direction   = new float3(0, k_GroundedTestDistance, 0),
					Orientation = transform.rot,
					Position    = transform.pos
				};


				if (cwBuffer.CastCollider(groundCastBefore, out var _) && !cwBuffer.CastCollider(groundCastAfter, out var _))
				{
					// make a projection first
					var sphereProbe = SphereCollider.Create(float3.zero, 0.5f, CollisionFilter.Default);
					var projCast = new ColliderDistanceInput
					{
						Collider    = (Collider*) sphereProbe.GetUnsafePtr(),
						MaxDistance = 0.1f,
						Transform   = new RigidTransform(transform.rot, transform.pos + new float3(prevVelocity.x, 0, prevVelocity.z) * dt * 4)
					};

					var projHits  = new NativeArray<DistanceHit>(128, Allocator.Temp);
					var collector = new CharacterControllerUtilities.MaxHitsCollector<DistanceHit>(0.0f, ref projHits);
					if (cwBuffer.CalculateDistance(projCast, ref collector))
					{
						for (var i = 0; i != collector.NumHits; i++)
						{
							var hit = projHits[i];
							if (hit.Position.y > transform.pos.y && (hit.Position.y - transform.pos.y) < 1f)
							{
								transform.pos.y = hit.Position.y;
								v.Value.y       = prevVelocity.y;
							}
						}
					}

					projHits.Dispose();

					sphereProbe.Release();
				}

				if (supportState != CharacterControllerUtilities.CharacterSupportState.Supported)
				{
					v.Value.y -= 20 * dt;
				}

				constraints.Dispose();
				distanceHits.Dispose();
				castHits.Dispose();

				t.Value = transform.pos;
			}
		}

		// because we are creating blob assets, we can't burst jobs with it, so we are obligated to create a new non-burst job.
		private struct UpdateCharacterQuery : IJobProcessComponentData<CustomCharacterController, PhysicsCollider>
		{
			public void Execute(ref CustomCharacterController cc, ref PhysicsCollider coll)
			{
				Assert.IsTrue(coll.ColliderPtr != null, "coll.ColliderPtr != null");
				Assert.IsTrue(coll.ColliderPtr->Type == ColliderType.Capsule, "coll.ColliderPtr->Type == ColliderType.Capsule");

				var capsuleColl = (CapsuleCollider*) coll.ColliderPtr;

				if (cc.QueryBigCapsule == BlobAssetReference<Collider>.Null
				    || !(capsuleColl->Radius + cc.SkinWidth).Equals(((CapsuleCollider*) cc.QueryBigCapsule.GetUnsafePtr())->Radius))
				{
					// release...
					if (cc.QueryBigCapsule != BlobAssetReference<Collider>.Null)
					{
						cc.QueryBigCapsule.Release();
						cc.QuerySmallCapsule.Release();
						cc.QueryBigSphere.Release();
						cc.QuerySmallSphere.Release();
					}

					Debug.Log($"Recreating with {capsuleColl->Radius} {cc.SkinWidth}");
					
					cc.QuerySmallCapsule = CapsuleCollider.Create(capsuleColl->Vertex0, capsuleColl->Vertex1, capsuleColl->Radius, capsuleColl->Filter);
					cc.QueryBigCapsule   = CapsuleCollider.Create(capsuleColl->Vertex0, capsuleColl->Vertex1, capsuleColl->Radius + cc.SkinWidth, capsuleColl->Filter);

					cc.QuerySmallSphere = SphereCollider.Create(float3.zero, capsuleColl->Radius, capsuleColl->Filter);
					cc.QueryBigSphere   = SphereCollider.Create(float3.zero, capsuleColl->Radius + cc.SkinWidth, capsuleColl->Filter);
				}
			}
		}

		//[BurstCompile]
		private struct MoveJobWithVelocityAll : IJobProcessComponentDataWithEntity<CustomCharacterController, Translation, Rotation, Velocity, PhysicsCollider>
		{
			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith> CollideWithBufferArray;

			[NativeDisableParallelForRestriction]
			public NativeList<MoveJob.OCCMoveVector> MoveVectors;
			[NativeDisableParallelForRestriction]
			public NativeList<CustomCharacterController.CollisionInfo> Collisions;
			[NativeDisableParallelForRestriction]
			public NativeArray<DistanceHit> PenetrationHits;

			public float DeltaTime;

			public void Execute(Entity entity, int index, ref CustomCharacterController cc, ref Translation t, ref Rotation r, ref Velocity v, ref PhysicsCollider coll)
			{
				var transform = RigidTransform(r.Value, t.Value);

				MoveJob.Result result = default;

				result.Collisions = Collisions;

				cc.SkinWidth                = 0.01f;
				cc.MaxSlope                 = 45f;
				cc.StepOffset               = 0.5f;
				cc.IsLocalHuman             = true;
				cc.MinSlowAgainstWallsAngle = 10;
				cc.SlowAgainstWalls         = true;

				v.Value.y = -20;

				new MoveJob
				{
					CwBuffer  = CollideWithBufferArray[entity],
					Data      = cc,
					Entity    = entity,
					Transform = transform,
					Collider  = (CapsuleCollider*) coll.ColliderPtr,

					MoveVector              = v.Value * DeltaTime,
					ForceTryToStickToGround = true,
					DoNotStepOffset         = false,
					SlideAlongCeiling       = true,
					SlideWhenMovingDown     = true,

					ResultAlloc = new UnsafeAllocation<MoveJob.Result>(ref result),

					// private things...
					MoveVectors     = MoveVectors,
					PenetrationHits = PenetrationHits
				}.Execute();

				t.Value = result.Transform.pos;
			}
		}

		public struct MoveJob : IJob
		{
			#region Utility

			private static float SqrMagnitudeFrom(float3 vectorA, float3 vectorB)
			{
				var diff = vectorA - vectorB;
				return lengthsq(diff);
			}

			// Is the movement vector almost zero (i.e. very small)?
			private static bool IsMoveVectorAlmostZero(float3 moveVector)
			{
				var aMv = abs(moveVector);

				return !(aMv.x > k_SmallMoveVector
				         || aMv.y > k_SmallMoveVector
				         || aMv.z > k_SmallMoveVector);
			}

			// Taken from Mathf, rewritten for burst support.
			private static bool Approximately(double a, double b)
			{
				return abs(b - a) < max(1E-06 * max(abs(a), abs(b)), DBL_MIN_NORMAL * 8f);
			}

			#endregion

			public struct Result
			{
				public float3 DownCollisionNormal;
				public bool   HasDownCollisionNormal;

				public RigidTransform Transform;

				public NativeList<CustomCharacterController.CollisionInfo> Collisions;

				public CollisionFlags CollisionFlags;
			}

			// A vector used by the OpenCharacterController.
			public struct OCCMoveVector
			{
				/// <summary>
				/// The move vector.
				/// Note: This gets used up during the move loop, so will be zero by the end of the loop.
				/// </summary>
				public float3 moveVector { get; set; }

				/// <summary>
				/// Can the movement slide along obstacles?
				/// </summary>
				public bool canSlide { get; set; }

				/// <summary>
				/// Constructor.
				/// </summary>
				/// <param name="newMoveVector">The move vector.</param>
				/// <param name="newCanSlide">Can the movement slide along obstacles?</param>
				public OCCMoveVector(float3 newMoveVector, bool newCanSlide = true)
					: this()
				{
					moveVector = newMoveVector;
					canSlide   = newCanSlide;
				}
			}

			public struct OCCStuckInfo
			{
				// For keeping track of the character's position, to determine when the character gets stuck.
				float3? m_StuckPosition;

				// Count how long the character is in the same position.
				int m_StuckPositionCount;

				// If character's position does not change by more than this amount then we assume the character is stuck.
				const float k_StuckDistance = 0.001f;

				// If character's position does not change by more than this amount then we assume the character is stuck.
				const float k_StuckSqrDistance = k_StuckDistance * k_StuckDistance;

				// If character collided this number of times during the movement loop then test if character is stuck by examining the position
				const int k_HitCountForStuck = 6;

				// Assume character is stuck if the position is the same for longer than this number of loop iterations
				const int k_MaxStuckPositionCount = 1;

				/// <summary>
				/// Is the character stuck in the current move loop iteration?
				/// </summary>
				public bool isStuck { get; set; }

				/// <summary>
				/// Count the number of collisions during movement, to determine when the character gets stuck.
				/// </summary>
				public int hitCount { get; set; }


				/// <summary>
				/// Called when the move loop starts.
				/// </summary>
				public void OnMoveLoop()
				{
					hitCount             = 0;
					m_StuckPositionCount = 0;
					m_StuckPosition      = null;
					isStuck              = false;
				}

				/// <summary>
				/// Is the character stuck during the movement loop (e.g. bouncing between 2 or more colliders)?
				/// </summary>
				/// <param name="characterPosition">The character's position.</param>
				/// <param name="currentMoveVector">Current move vector.</param>
				/// <param name="originalMoveVector">Original move vector.</param>
				/// <returns></returns>
				public bool UpdateStuck(Vector3 characterPosition, Vector3 currentMoveVector,
				                        Vector3 originalMoveVector)
				{
					// First test
					if (!isStuck)
					{
						// From Quake2: "if velocity is against the original velocity, stop dead to avoid tiny occilations in sloping corners"
						if (abs(lengthsq(currentMoveVector.sqrMagnitude)) > FLT_MIN_NORMAL &&
						    Vector3.Dot(currentMoveVector, originalMoveVector) <= 0.0f)
						{
							isStuck = true;
						}
					}

					// Second test
					if (!isStuck)
					{
						// Test if collided and while position remains the same
						if (hitCount < k_HitCountForStuck)
						{
							return false;
						}

						if (m_StuckPosition == null)
						{
							m_StuckPosition = characterPosition;
						}
						else if (SqrMagnitudeFrom(m_StuckPosition.Value, characterPosition) <= k_StuckSqrDistance)
						{
							m_StuckPositionCount++;
							if (m_StuckPositionCount > k_MaxStuckPositionCount)
							{
								isStuck = true;
							}
						}
						else
						{
							m_StuckPositionCount = 0;
							m_StuckPosition      = null;
						}
					}

					if (isStuck)
					{
						isStuck              = false;
						hitCount             = 0;
						m_StuckPositionCount = 0;
						m_StuckPosition      = null;

						return true;
					}

					return false;
				}
			}

			public DynamicBuffer<CollideWith> CwBuffer;
			public Entity                     Entity;
			public CustomCharacterController  Data;
			public CapsuleCollider*           Collider;

			public float3 MoveVector;
			public bool   ForceTryToStickToGround, SlideWhenMovingDown, DoNotStepOffset, SlideAlongCeiling;

			public RigidTransform Transform;

			public UnsafeAllocation<Result> ResultAlloc;

			public void DoGroundCast()
			{
				m_IsGrounded = CheckCollisionBelow(k_GroundedTestDistance,
					Transform.pos,
					float3.zero,
					Data.IsLocalHuman,
					Data.IsLocalHuman);
			}

			public float scaledRadius;

			private float3         m_StartPosition;
			private CollisionFlags m_CollisionFlags;
			private bool           m_IsGrounded;
			private OCCStuckInfo   m_StuckInfo;

			// Movement vectors used in the move loop.
			public NativeList<OCCMoveVector> MoveVectors;
			public NativeArray<DistanceHit>  PenetrationHits;

			// Next index in the moveVectors list.
			private int m_NextMoveVectorIndex;

			private float m_SlopeMovementOffset;
			private float m_InvRescaleFactor;

			public void Execute()
			{
				MoveVectors.Clear();
				
				m_StuckInfo = default;

				scaledRadius          = 1f;
				m_SlopeMovementOffset = Data.StepOffset / tan(radians(Data.MaxSlope));
				m_InvRescaleFactor    = 1 / cos(radians(Data.MinSlowAgainstWallsAngle));

				DoGroundCast();

				var wasGrounded   = m_IsGrounded;
				var moveVectorNoY = new float3(MoveVector.x, 0.0f, MoveVector.z);

				if (m_IsGrounded)
					MoveVector.y = 0;

				var tryToStickToGround = wasGrounded && (ForceTryToStickToGround || (MoveVector.y <= 0.0f && abs(lengthsq(moveVectorNoY)) > FLT_MIN_NORMAL));

				m_StartPosition  = Transform.pos;
				m_CollisionFlags = CollisionFlags.None;

				// todo: Stop sliding down slopes when character jumps

				// Do the move loop
				MoveLoop(MoveVector, tryToStickToGround, SlideWhenMovingDown, DoNotStepOffset);

				var doDownCast = tryToStickToGround ||
				                 MoveVector.y <= 0.0f;
				UpdateGrounded(ResultAlloc.Value.CollisionFlags, doDownCast);

				ref var result = ref ResultAlloc.AsRef();

				result.Transform = Transform;
			}

			// Determine if the character is grounded.
			// 		movedCollisionFlags: Moved collision flags of the current move. Set to None if not moving.
			// 		doDownCast: Do a down cast? We want to avoid this when the character is jumping upwards.
			void UpdateGrounded(CollisionFlags movedCollisionFlags, bool doDownCast = true)
			{
				if ((movedCollisionFlags & CollisionFlags.CollidedBelow) != 0)
				{
					m_IsGrounded = true;
				}
				else if (doDownCast)
				{
					DoGroundCast();
				}
				else
				{
					m_IsGrounded = false;
				}
			}

			public bool CheckCollisionBelow(float  distance, float3 currentPosition,
			                                float3 offsetPosition,
			                                bool   useSecondSphereCast    = false,
			                                bool   adjustPositionSlightly = false)
			{
				var didCollide    = false;
				var extraDistance = adjustPositionSlightly ? k_CollisionOffset : 0.0f;

				if (SmallSphereCast(Vector3.down,
					Data.SkinWidth + distance,
					currentPosition,
					out var hitInfo,
					offsetPosition,
					true))
				{
					didCollide = true;
				}

				if (!didCollide && useSecondSphereCast)
				{
					if (BigSphereCast(-up(),
						distance + extraDistance, currentPosition,
						out hitInfo,
						offsetPosition + up() * extraDistance,
						true))
					{
						didCollide = true;
					}
				}

				return didCollide;
			}

			// Movement loop. Keep moving until completely blocked by obstacles, or we reached the desired position/distance.
			// 		moveVector: The move vector.
			// 		tryToStickToGround: Try to stick to the ground?
			// 		slideWhenMovingDown: Slide against obstacles when moving down? (e.g. we don't want to slide when applying gravity while the charcter is grounded)
			// 		doNotStepOffset: Do not try to perform the step offset?
			void MoveLoop(Vector3 moveVector, bool tryToStickToGround, bool slideWhenMovingDown, bool doNotStepOffset)
			{
				MoveVectors.Clear();
				m_NextMoveVectorIndex = 0;

				// Split the move vector into horizontal and vertical components.
				SplitMoveVector(moveVector, slideWhenMovingDown, doNotStepOffset);
				var remainingMoveVector = MoveVectors[m_NextMoveVectorIndex];
				m_NextMoveVectorIndex++;

				var didTryToStickToGround = false;
				m_StuckInfo.OnMoveLoop();
				var virtualPosition = Transform.pos;

				// The loop
				for (var i = 0; i < k_MaxMoveIterations; i++)
				{
					var refMoveVector = remainingMoveVector.moveVector;
					var collided      = MoveMajorStep(ref refMoveVector, remainingMoveVector.canSlide, didTryToStickToGround, ref virtualPosition);

					remainingMoveVector.moveVector = refMoveVector;

					// Character stuck?
					if (m_StuckInfo.UpdateStuck(virtualPosition, remainingMoveVector.moveVector, moveVector))
					{
						// Stop current move loop vector
						remainingMoveVector = new OCCMoveVector(float3.zero);
					}
					else if (!Data.IsLocalHuman && collided)
					{
						// Only slide once for non-human controlled characters
						remainingMoveVector.canSlide = false;
					}

					// Not collided OR vector used up (i.e. vector is zero)?
					if (!collided || lengthsq(remainingMoveVector.moveVector) < FLT_MIN_NORMAL)
					{
						// Are there remaining movement vectors?
						if (m_NextMoveVectorIndex < MoveVectors.Length)
						{
							remainingMoveVector = MoveVectors[m_NextMoveVectorIndex];
							m_NextMoveVectorIndex++;
						}
						else
						{
							if (!tryToStickToGround || didTryToStickToGround)
							{
								break;
							}

							// Try to stick to the ground
							didTryToStickToGround = true;
							if (!CanStickToGround(moveVector, out remainingMoveVector))
							{
								break;
							}
						}
					}
				}

				Transform.pos = virtualPosition;
			}

			// Move the capsule position.
			// 		moveVector: Move vector.
			// 		collideDirection: Direction we encountered collision. Null if no collision.
			//		hitInfo: Hit info of the collision. Null if no collision.
			//		currentPosition: position of the character
			void MovePosition(float3 moveVector, Vector3? collideDirection, ColliderCastHit? hitInfo, ref float3 currentPosition)
			{
				if (lengthsq(moveVector) > FLT_MIN_NORMAL)
				{
					currentPosition += moveVector;
				}

				if (collideDirection != null && hitInfo != null)
				{
					//UpdateCollisionInfo(collideDirection.Value, hitInfo.Value, currentPosition);
				}
			}

			// A single movement major step. Returns true when there is collision.
			//		moveVector: The move vector.
			// 		canSlide: Can slide against obstacles?
			// 		tryGrounding: Try grounding the player?
			//		currentPosition: position of the character
			bool MoveMajorStep(ref float3 moveVector, bool canSlide, bool tryGrounding, ref float3 currentPosition)
			{
				var             direction = normalizesafe(moveVector);
				var             distance  = length(moveVector);
				ColliderCastHit bigRadiusHitInfo;
				ColliderCastHit smallRadiusHitInfo;
				bool            smallRadiusHit;
				bool            bigRadiusHit;

				if (!CapsuleCast(direction, distance, currentPosition,
					out smallRadiusHit, out bigRadiusHit,
					out smallRadiusHitInfo, out bigRadiusHitInfo,
					Vector3.zero))
				{
					// No collision, so move to the position
					MovePosition(moveVector, null, null, ref currentPosition);

					// Check for penetration
					float  penetrationDistance;
					float3 penetrationDirection;
					if (GetPenetrationInfo(out penetrationDistance, out penetrationDirection, currentPosition))
					{
						// Push away from obstacles
						MovePosition(penetrationDirection * penetrationDistance, null, null, ref currentPosition);
					}

					// Stop current move loop vector
					moveVector = Vector3.zero;

					return false;
				}

				var smallRadiusDistance = getDist(smallRadiusHitInfo, distance + scaledRadius);
				var bigRadiusDistance   = getDist(bigRadiusHitInfo, distance + scaledRadius + Data.SkinWidth);

				// Did the big radius not hit an obstacle?
				if (!bigRadiusHit)
				{
					// The small radius hit an obstacle, so character is inside an obstacle
					MoveAwayFromObstacle(ref moveVector, ref smallRadiusHitInfo, smallRadiusDistance,
						direction, distance,
						canSlide,
						tryGrounding,
						true, ref currentPosition);

					return true;
				}

				// Use the nearest collision point (e.g. to handle cases where 2 or more colliders' edges meet)
				if (smallRadiusHit && smallRadiusDistance < bigRadiusDistance)
				{
					MoveAwayFromObstacle(ref moveVector, ref smallRadiusHitInfo, smallRadiusDistance,
						direction, distance,
						canSlide,
						tryGrounding,
						true, ref currentPosition);
					return true;
				}

				MoveAwayFromObstacle(ref moveVector, ref bigRadiusHitInfo, bigRadiusDistance,
					direction, distance,
					canSlide,
					tryGrounding,
					false, ref currentPosition);

				return true;
			}

			// Called when a capsule cast detected an obstacle. Move away from the obstacle and slide against it if needed.
			// 		moveVector: The movement vector.
			// 		hitInfoCapsule: Hit info of the capsule cast collision.
			// 		direction: Direction of the cast.
			// 		distance: Distance of the cast.
			// 		canSlide: Can slide against obstacles?
			// 		tryGrounding: Try grounding the player?
			// 		hitSmallCapsule: Did the collision occur with the small capsule (i.e. no skin width)?
			//		currentPosition: position of the character
			void MoveAwayFromObstacle(ref float3 moveVector, ref ColliderCastHit hitInfoCapsule, float hitInfoDistance,
			                          float3     direction,  float               distance,
			                          bool       canSlide,
			                          bool       tryGrounding,
			                          bool       hitSmallCapsule, ref float3 currentPosition)
			{
				// IMPORTANT: This method must set moveVector.

				// When the small capsule hit then stop skinWidth away from obstacles
				var collisionOffset = hitSmallCapsule ? Data.SkinWidth : k_CollisionOffset;

				var hitDistance = max(hitInfoDistance - collisionOffset, 0.0f);
				// Note: remainingDistance is more accurate is we use hitDistance, but using hitInfoCapsule.distance gives a tiny 
				// bit of dampening when sliding along obstacles
				var remainingDistance = max(distance - hitInfoDistance, 0.0f);

				// Move to the collision point
				MovePosition(direction * hitDistance, direction, hitInfoCapsule, ref currentPosition);

				float3     hitNormal;
				RaycastHit hitInfoRay;
				var        rayOrigin    = currentPosition + Collider->CalculateAabb().Center;
				var        rayDirection = hitInfoCapsule.Position - rayOrigin;

				// Raycast returns a more accurate normal than SphereCast/CapsuleCast
				// Using angle <= k_MaxAngleToUseRaycastNormal gives a curve when collision is near an edge.
				var rayInput = new RaycastInput
				{
					Filter = Collider->Filter,
					Ray    = new Unity.Physics.Ray(rayOrigin, rayDirection * k_RaycastScaleDistance)
				};

				if (CwBuffer.CastRay(in rayInput, out hitInfoRay)
				    && hitInfoRay.RigidBodyIndex == hitInfoCapsule.RigidBodyIndex
				    && Vector3.Angle(hitInfoCapsule.SurfaceNormal, hitInfoRay.SurfaceNormal) <= k_MaxAngleToUseRaycastNormal)
				{
					hitNormal = hitInfoRay.SurfaceNormal;
				}
				else
				{
					hitNormal = hitInfoCapsule.SurfaceNormal;
				}

				float  penetrationDistance;
				float3 penetrationDirection;

				if (GetPenetrationInfo(out penetrationDistance, out penetrationDirection, currentPosition, true, null, hitInfoCapsule))
				{
					// Push away from the obstacle
					MovePosition(penetrationDirection * penetrationDistance, null, null, ref currentPosition);
				}

				var slopeIsSteep = false;
				if (tryGrounding || m_StuckInfo.isStuck)
				{
					// No further movement when grounding the character, or the character is stuck
					canSlide = false;
				}
				else if (abs(moveVector.x) > FLT_MIN_NORMAL || abs(moveVector.z) > FLT_MIN_NORMAL)
				{
					// Test if character is trying to walk up a steep slope
					var slopeAngle = Vector3.Angle(up(), hitNormal);
					slopeIsSteep = slopeAngle > Data.MaxSlope && slopeAngle < k_MaxSlopeLimit && Vector3.Dot(direction, hitNormal) < 0.0f;
				}

				// Set moveVector
				if (canSlide && remainingDistance > 0.0f)
				{
					var slideNormal = hitNormal;

					if (slopeIsSteep && slideNormal.y > 0.0f)
					{
						// Do not move up the slope
						slideNormal.y = 0.0f;
						slideNormal   = normalizesafe(slideNormal);
					}

					// Vector to slide along the obstacle
					var project = Vector3.Cross(direction, slideNormal);
					project = Vector3.Cross(slideNormal, project);

					if (slopeIsSteep && project.y > 0.0f)
					{
						// Do not move up the slope
						project.y = 0.0f;
					}

					project.Normalize();

					// Slide along the obstacle
					var isWallSlowingDown = Data.SlowAgainstWalls && Data.MinSlowAgainstWallsAngle < 90.0f;

					if (isWallSlowingDown)
					{
						// Cosine of angle between the movement direction and the tangent is equivalent to the sin of
						// the angle between the movement direction and the normal, which is the sliding component of
						// our movement.
						var cosine         = Vector3.Dot(project, direction);
						var slowDownFactor = Mathf.Clamp01(cosine * m_InvRescaleFactor);

						moveVector = project * (remainingDistance * slowDownFactor);
					}
					else
					{
						// No slow down, keep the same speed even against walls.
						moveVector = project * remainingDistance;
					}
				}
				else
				{
					// Stop current move loop vector
					moveVector = float3.zero;
				}

				if (direction.y < 0.0f && Approximately(direction.x, 0.0f) && Approximately(direction.z, 0.0f))
				{
					// This is used by the sliding down slopes
					ref var result = ref ResultAlloc.AsRef();

					result.DownCollisionNormal = hitNormal;
				}
			}

			// Get direction and distance to move out of the obstacle.
			// 		getDistance: Get distance to move out of the obstacle.
			// 		getDirection: Get direction to move out of the obstacle.
			//		currentPosition: position of the character
			// 		includeSkinWidth: Include the skin width in the test?
			// 		offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
			// 		hitInfo: The hit info.
			bool GetPenetrationInfo(out float        getDistance, out float3 getDirection, float3 currentPosition,
			                        bool             includeSkinWidth = true,
			                        float3?          offsetPosition   = null,
			                        ColliderCastHit? hitInfo          = null)
			{
				getDistance  = 0.0f;
				getDirection = Vector3.zero;

				var offset        = offsetPosition != null ? offsetPosition.Value : float3.zero;

				var maxCollector = new CharacterControllerUtilities.MaxHitsCollector<DistanceHit>(0f, ref PenetrationHits);
				var distanceInput = new ColliderDistanceInput
				{
					Collider    = (Collider*) (includeSkinWidth ? Data.QueryBigCapsule : Data.QuerySmallCapsule).GetUnsafePtr(),
					MaxDistance = k_MinMoveSqrDistance,
					Transform   = new RigidTransform(Transform.rot, currentPosition + offset)
				};

				if (!CwBuffer.CalculateDistance(distanceInput, ref maxCollector))
					return false;

				var result   = false;
				var localPos = float3.zero;
				for (var i = 0; i < maxCollector.NumHits; i++)
				{
					var distanceHit = PenetrationHits[i];

					if (distanceHit.Fraction < 0) // inside
					{						
						localPos += -distanceHit.SurfaceNormal * (distanceHit.Distance + k_CollisionOffset);
						result   =  true;
					}
					else if (hitInfo != null && hitInfo.Value.RigidBodyIndex == distanceHit.RigidBodyIndex)
					{
						// We can use the hit normal to push away from the collider, because CapsuleCast generally returns a normal
						// that pushes away from the collider.
						localPos += -hitInfo.Value.SurfaceNormal * k_CollisionOffset;
						result   =  true;
					}
				}

				if (!result)
					return false;

				getDistance  = length(localPos);
				getDirection = normalizesafe(localPos);

				return true;
			}


			// Test if character can stick to the ground, and set the down vector if so.
			// 		moveVector: The original movement vector.
			// 		getDownVector: Get the down vector.
			private bool CanStickToGround(float3 moveVector, out OCCMoveVector getDownVector)
			{
				var moveVectorNoY = new float3(moveVector.x, 0.0f, moveVector.z);
				var downDistance  = max(length(moveVectorNoY), k_MinStickToGroundDownDistance);
				if (moveVector.y < 0.0f)
				{
					downDistance = max(downDistance, abs(moveVector.y));
				}

				if (downDistance <= k_MaxStickToGroundDownDistance)
				{
					getDownVector = new OCCMoveVector(float3(0, -1, 0) * downDistance, false);
					return true;
				}

				getDownVector = new OCCMoveVector(float3.zero);
				return false;
			}


			// Split the move vector into horizontal and vertical components. The results are added to the moveVectors list.
			// 		moveVector: The move vector.
			// 		slideWhenMovingDown: Slide against obstacles when moving down? (e.g. we don't want to slide when applying gravity while the character is grounded)
			// 		doNotStepOffset: Do not try to perform the step offset?
			private void SplitMoveVector(Vector3 moveVector, bool slideWhenMovingDown, bool doNotStepOffset)
			{
				var horizontal             = new Vector3(moveVector.x, 0.0f, moveVector.z);
				var vertical               = new Vector3(0.0f, moveVector.y, 0.0f);
				var horizontalIsAlmostZero = IsMoveVectorAlmostZero(horizontal);
				var tempStepOffset         = Data.StepOffset;
				var doStepOffset = m_IsGrounded &&
				                   !doNotStepOffset &&
				                   !Approximately(tempStepOffset, 0.0f) &&
				                   !horizontalIsAlmostZero;

				// Note: Vector is split in this order: up, horizontal, down

				if (vertical.y > 0.0f)
				{
					// Up
					if (abs(horizontal.x) > FLT_MIN_NORMAL || abs(horizontal.z) > FLT_MIN_NORMAL)
					{
						// Move up then horizontal
						AddMoveVector(vertical, SlideAlongCeiling);
						AddMoveVector(horizontal);
					}
					else
					{
						// Move up
						AddMoveVector(vertical, SlideAlongCeiling);
					}
				}
				else if (vertical.y < 0.0f)
				{
					// Down
					if (abs(horizontal.x) > FLT_MIN_NORMAL || abs(horizontal.z) > FLT_MIN_NORMAL)
					{
						if (doStepOffset && CanStepOffset(horizontal))
						{
							// Move up, horizontal then down
							AddMoveVector(Vector3.up * tempStepOffset, false);
							AddMoveVector(horizontal);
							if (slideWhenMovingDown)
							{
								AddMoveVector(vertical);
								AddMoveVector(Vector3.down * tempStepOffset);
							}
							else
							{
								AddMoveVector(vertical + Vector3.down * tempStepOffset);
							}
						}
						else
						{
							// Move horizontal then down
							AddMoveVector(horizontal);
							AddMoveVector(vertical, slideWhenMovingDown);
						}
					}
					else
					{
						// Move down
						AddMoveVector(vertical, slideWhenMovingDown);
					}
				}
				else
				{
					// Horizontal
					if (doStepOffset && CanStepOffset(horizontal))
					{
						// Move up, horizontal then down
						AddMoveVector(float3(0, 1, 0) * tempStepOffset, false);
						AddMoveVector(horizontal);
						AddMoveVector(float3(0, -1, 0) * tempStepOffset);
					}
					else
					{
						// Move horizontal
						AddMoveVector(horizontal);
					}
				}
			}

			// Add the movement vector to the moveVectors list.
			// 		moveVector: Move vector to add.
			// 		canSlide: Can the movement slide along obstacles?
			void AddMoveVector(float3 moveVector, bool canSlide = true)
			{
				MoveVectors.Add(new OCCMoveVector(moveVector, canSlide));
			}


			// Can the character perform a step offset?
			// 		moveVector: Horizontal movement vector.
			bool CanStepOffset(float3 moveVector)
			{
				var             moveVectorMagnitude = length(moveVector);
				var             position            = Transform.pos;
				ColliderCastHit hitInfo;

				// Only step up if there's an obstacle at the character's feet (e.g. do not step when only character's head collides)
				if (!SmallSphereCast(moveVector, moveVectorMagnitude, position, out hitInfo, Vector3.zero, true) &&
				    !BigSphereCast(moveVector, moveVectorMagnitude, position, out hitInfo, Vector3.zero, true))
				{
					return false;
				}

				var upDistance = Mathf.Max(Data.StepOffset, k_MinStepOffsetHeight);

				// We only step over obstacles if we can partially fit on it (i.e. fit the capsule's radius)
				var horizontal     = moveVector * scaledRadius;
				var horizontalSize = length(horizontal);
				horizontal = normalizesafe(horizontal);

				// Any obstacles ahead (after we moved up)?
				var up = Vector3.up * upDistance;
				if (SmallCapsuleCast(horizontal, Data.SkinWidth + horizontalSize, out hitInfo, up, position) ||
				    BigCapsuleCast(horizontal, horizontalSize, out hitInfo, up, position))
				{
					return false;
				}

				return !CheckSteepSlopeAhead(moveVector);
			}

			// Returns true if there's a steep slope ahead.
			//		moveVector: The movement vector.
			// 		alsoCheckForStepOffset: Do a second test where the step offset will move the player to?
			bool CheckSteepSlopeAhead(Vector3 moveVector, bool alsoCheckForStepOffset = true)
			{
				var direction = moveVector.normalized;
				var distance  = moveVector.magnitude;

				if (CheckSteepSlopAhead(direction, distance, Vector3.zero))
				{
					return true;
				}

				// Only need to do the second test for human controlled character
				if (!alsoCheckForStepOffset || !Data.IsLocalHuman)
				{
					return false;
				}

				// Check above where the step offset will move the player to
				return CheckSteepSlopAhead(direction,
					Mathf.Max(distance, k_MinCheckSteepSlopeAheadDistance),
					Vector3.up * Data.StepOffset);
			}

			float getDist(ColliderCastHit hit, float originalDistance)
			{
				return hit.Fraction * originalDistance;
			}

			// Returns true if there's a steep slope ahead.
			bool CheckSteepSlopAhead(float3 direction, float distance, float3 offsetPosition)
			{
				ColliderCastHit bigRadiusHitInfo;
				ColliderCastHit smallRadiusHitInfo;
				bool            smallRadiusHit;
				bool            bigRadiusHit;

				float3 surfaceNormal;

				if (!CapsuleCast(direction, distance, Transform.pos,
					out smallRadiusHit, out bigRadiusHit,
					out smallRadiusHitInfo, out bigRadiusHitInfo,
					offsetPosition))
				{
					// No collision
					return false;
				}

				var smallRadiusDistance = getDist(smallRadiusHitInfo, distance + scaledRadius);
				var bigRadiusDistance   = getDist(bigRadiusHitInfo, distance + scaledRadius + Data.SkinWidth);
				var hitInfoCapsule      = !bigRadiusHit || (smallRadiusHit && smallRadiusDistance < bigRadiusDistance) ? smallRadiusHitInfo : bigRadiusHitInfo;

				RaycastHit hitInfoRay;
				var        rayOrigin = Transform.pos + Collider->CalculateAabb().Center + offsetPosition;

				var offset       = Mathf.Clamp(m_SlopeMovementOffset, 0.0f, distance * k_SlopeCheckDistanceMultiplier);
				var rayDirection = (hitInfoCapsule.Position + direction * offset) - rayOrigin;

				// Raycast returns a more accurate normal than SphereCast/CapsuleCast
				var rayInput = new RaycastInput
				{
					Filter = Collider->Filter,
					Ray    = new Unity.Physics.Ray(rayOrigin, rayDirection * k_RaycastScaleDistance)
				};

				if (CwBuffer.CastRay(in rayInput, out hitInfoRay))
				{
					surfaceNormal = hitInfoRay.SurfaceNormal;
				}
				else
				{
					return false;
				}

				var slopeAngle = Vector3.Angle(Vector3.up, surfaceNormal);
				var slopeIsSteep = slopeAngle > Data.MaxSlope &&
				                   slopeAngle < k_MaxSlopeLimit &&
				                   Vector3.Dot(direction, surfaceNormal) < 0.0f;

				return slopeIsSteep;
			}


			// Do two capsule casts. One excluding the capsule's skin width and one including the skin width.
			// 		direction: Direction to cast
			// 		distance: Distance to cast
			//		currentPosition: position of the character
			// 		smallRadiusHit: Did hit, excluding the skin width?
			// 		bigRadiusHit: Did hit, including the skin width?
			// 		smallRadiusHitInfo: Hit info for cast excluding the skin width.
			// 		bigRadiusHitInfo: Hit info for cast including the skin width.
			// 		offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
			bool CapsuleCast(float3              direction,          float               distance, float3 currentPosition,
			                 out bool            smallRadiusHit,     out bool            bigRadiusHit,
			                 out ColliderCastHit smallRadiusHitInfo, out ColliderCastHit bigRadiusHitInfo,
			                 float3              offsetPosition)
			{
				// Exclude the skin width in the test
				smallRadiusHit = SmallCapsuleCast(direction, distance, out smallRadiusHitInfo, offsetPosition, currentPosition);

				// Include the skin width in the test
				bigRadiusHit = BigCapsuleCast(direction, distance, out bigRadiusHitInfo, offsetPosition, currentPosition);

				return smallRadiusHit || bigRadiusHit;
			}

			// Do a capsule cast, excluding the skin width.
			//		direction: Direction to cast.
			// 		distance: Distance to cast.
			// 		smallRadiusHitInfo: Hit info.
			// 		offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
			//		currentPosition: position of the character
			bool SmallCapsuleCast(float3              direction, float distance,
			                      out ColliderCastHit smallRadiusHitInfo,
			                      float3              offsetPosition, float3 currentPosition)
			{
				// Cast further than the distance we need, to try to take into account small edge cases (e.g. Casts fail 
				// when moving almost parallel to an obstacle for small distances).
				var extraDistance = scaledRadius;

				var input = new ColliderCastInput
				{
					Collider    = (Collider*) Data.QuerySmallCapsule.GetUnsafePtr(),
					Direction   = direction * (distance + extraDistance),
					Orientation = quaternion.identity,
					Position    = currentPosition + offsetPosition
				};

				if (CwBuffer.CastCollider(in input, out smallRadiusHitInfo))
				{
					return smallRadiusHitInfo.Fraction * (distance + extraDistance) <= distance;
				}

				return false;
			}

			// Do a capsule cast, includes the skin width.
			//		direction: Direction to cast.
			// 		distance: Distance to cast.
			// 		bigRadiusHitInfo: Hit info.
			// 		offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
			//		currentPosition: position of the character
			bool BigCapsuleCast(float3              direction, float distance,
			                    out ColliderCastHit bigRadiusHitInfo,
			                    float3              offsetPosition, float3 currentPosition)
			{
				// Cast further than the distance we need, to try to take into account small edge cases (e.g. Casts fail 
				// when moving almost parallel to an obstacle for small distances).
				var extraDistance = scaledRadius + Data.SkinWidth;

				var input = new ColliderCastInput
				{
					Collider    = (Collider*) Data.QuerySmallCapsule.GetUnsafePtr(),
					Direction   = direction * (distance + extraDistance),
					Orientation = quaternion.identity,
					Position    = currentPosition + offsetPosition
				};

				if (CwBuffer.CastCollider(in input, out bigRadiusHitInfo))
				{
					return bigRadiusHitInfo.Fraction * (distance + extraDistance) <= distance;
				}

				return false;
			}

			// Do a sphere cast, excludes the skin width. Sphere position is at the top or bottom of the capsule.
			// 		direction: Direction to cast.
			// 		distance: Distance to cast.
			// 		smallRadiusHitInfo: Hit info.
			// 		offsetPosition: Offset position, if we want to test somewhere relative to the capsule's position.
			// 		useBottomSphere: Use the sphere at the bottom of the capsule? If false then use the top sphere.
			//		currentPosition: position of the character
			bool SmallSphereCast(float3              direction, float distance, float3 currentPosition,
			                     out ColliderCastHit smallRadiusHitInfo,
			                     float3              offsetPosition,
			                     bool                useBottomSphere)
			{
				// Cast further than the distance we need, to try to take into account small edge cases (e.g. Casts fail 
				// when moving almost parallel to an obstacle for small distances).
				var extraDistance = scaledRadius;

				var spherePosition = useBottomSphere
					? GetBottomSphereWorldPosition(currentPosition) + offsetPosition
					: GetTopSphereWorldPosition(currentPosition) + offsetPosition;

				var input = new ColliderCastInput
				{
					Collider    = (Collider*) Data.QuerySmallSphere.GetUnsafePtr(),
					Direction   = direction * (distance + extraDistance),
					Orientation = quaternion.identity,
					Position    = spherePosition
				};

				if (CwBuffer.CastCollider(in input, out smallRadiusHitInfo))
				{
					return smallRadiusHitInfo.Fraction * (distance + extraDistance) <= distance;
				}

				return false;
			}

			// Do a sphere cast, including the skin width. Sphere position is at the top or bottom of the capsule.
			// 		direction">Direction to cast.
			// 		distance">Distance to cast.
			//		currentPosition: position of the character
			// 		bigRadiusHitInfo">Hit info.
			// 		offsetPosition">Offset position, if we want to test somewhere relative to the capsule's position.
			// 		useBottomSphere">Use the sphere at the bottom of the capsule? If false then use the top sphere.
			bool BigSphereCast(float3              direction, float distance, float3 currentPosition,
			                   out ColliderCastHit bigRadiusHitInfo,
			                   float3              offsetPosition,
			                   bool                useBottomSphere)
			{
				// Cast further than the distance we need, to try to take into account small edge cases (e.g. Casts fail 
				// when moving almost parallel to an obstacle for small distances).
				var extraDistance = scaledRadius + Data.SkinWidth;

				var spherePosition = useBottomSphere
					? GetBottomSphereWorldPosition(currentPosition) + offsetPosition
					: GetTopSphereWorldPosition(currentPosition) + offsetPosition;

				var input = new ColliderCastInput
				{
					Collider    = (Collider*) Data.QueryBigSphere.GetUnsafePtr(),
					Direction   = direction * (distance + extraDistance),
					Orientation = quaternion.identity,
					Position    = spherePosition
				};

				if (CwBuffer.CastCollider(in input, out bigRadiusHitInfo))
				{
					return bigRadiusHitInfo.Fraction * (distance + extraDistance) <= distance;
				}

				return false;
			}

			/// <summary>
			/// Get the top sphere's world position.
			/// </summary>
			float3 GetTopSphereWorldPosition(float3 position)
			{
				return position + Collider->Vertex1;
			}

			/// <summary>
			/// Get the bottom sphere's world position.
			/// </summary>
			float3 GetBottomSphereWorldPosition(float3 position)
			{
				return position + Collider->Vertex0;
			}
		}

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			/*var spCollider1 = (Collider*) SphereCollider.Create(float3.zero, 0.75f).GetUnsafePtr();
			var spCollider2 = (Collider*) SphereCollider.Create(new float3(0, 0.25f, 0), 0.75f).GetUnsafePtr();

			var di = new ColliderDistanceInput
			{
				Collider    = spCollider2,
				MaxDistance = 0.1f,
				Transform   = new RigidTransform(quaternion.identity, float3.zero)
			};
			
			spCollider1->CalculateDistance(di, out var closestHit);
			Debug.Log(closestHit.Position + ", " + closestHit.Fraction + ", " + closestHit.Distance)*/
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			//return inputDeps;

			inputDeps.Complete();

			var transformCollideWithBufferSystem = World.GetExistingManager<TransformCollideWithBufferSystem>();
			transformCollideWithBufferSystem.Update();

			//return new Job{CollideWithBufferArray = GetBufferFromEntity<CollideWith>(), dt = Time.deltaTime}.Schedule(this, inputDeps);
			//new MoveJob {CollideWithBufferArray = GetBufferFromEntity<CollideWith>(), dt = Time.deltaTime, PhysicsWorld = World.GetExistingManager<BuildPhysicsWorld>().PhysicsWorld}.Run(this);
			var updateQueryJob = new UpdateCharacterQuery();

			inputDeps = updateQueryJob.Schedule(this);
			
			var moveJob = new MoveJobWithVelocityAll
			{
				CollideWithBufferArray = GetBufferFromEntity<CollideWith>(),
				DeltaTime              = Time.deltaTime,

				Collisions      = new NativeList<CustomCharacterController.CollisionInfo>(Allocator.TempJob),
				MoveVectors     = new NativeList<MoveJob.OCCMoveVector>(Allocator.TempJob),
				PenetrationHits = new NativeArray<DistanceHit>(k_MaxOverlapColliders, Allocator.TempJob)
			};

			inputDeps = moveJob.Schedule(this, inputDeps);
			inputDeps.Complete();
			
			moveJob.Collisions.Dispose();
			moveJob.MoveVectors.Dispose();
			moveJob.PenetrationHits.Dispose();
			

			return inputDeps;
		}
	}
}