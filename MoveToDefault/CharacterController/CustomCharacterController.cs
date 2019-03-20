using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;

namespace Patapon4TLB.Default
{
	public struct CustomCharacterController : IComponentData
	{
		public float StepOffset;
		public float MaxSlope;
	}

	public unsafe class CustomCharacterControllerSystem : JobComponentSystem
	{
		private const float k_GroundCheckDistance = -0.0001f;

		private struct Job : IJobProcessComponentDataWithEntity<CustomCharacterController, Translation, Rotation, Velocity, PhysicsCollider>
		{
			public float dt;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith> CollideWithBufferArray;

			[ReadOnly]
			public PhysicsWorld PhysicsWorld;

			public void Execute(Entity e, int _, ref CustomCharacterController cc, ref Translation t, ref Rotation r, ref Velocity v, ref PhysicsCollider coll)
			{
				var cwBuffer = CollideWithBufferArray[e];

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

				v.Value.y = -10;

				CharacterControllerUtilities.CheckSupport(cwBuffer, PhysicsWorld, dt, transform, -math.up(), 1.57f,
					0.1f, capsuleColliderForQueries, ref constraints, ref distanceHits, out var supportState);

				CharacterControllerUtilities.CollideAndIntegrate(cwBuffer, PhysicsWorld, dt, 32, math.up(), new float3(0, -10, 0),
					1.0f, tau, damping, capsuleColliderForQueries,
					ref distanceHits, ref castHits, ref constraints,
					ref transform, ref v.Value);
				
				constraints.Dispose();
				distanceHits.Dispose();
				castHits.Dispose();

				t.Value = transform.pos;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			//return inputDeps;
			
			var transformCollideWithBufferSystem = World.GetExistingManager<TransformCollideWithBufferSystem>();
			transformCollideWithBufferSystem.Update();
			
			//return new Job{CollideWithBufferArray = GetBufferFromEntity<CollideWith>(), dt = Time.deltaTime}.Schedule(this, inputDeps);
			new Job {CollideWithBufferArray = GetBufferFromEntity<CollideWith>(), dt = Time.deltaTime, PhysicsWorld = World.GetExistingManager<BuildPhysicsWorld>().PhysicsWorld}.Run(this);
			return inputDeps;
		}
	}
}