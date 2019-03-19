using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

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
		
		private struct Job : IJobProcessComponentDataWithEntity<CustomCharacterController, Translation, Rotation, PhysicsVelocity, PhysicsCollider>
		{
			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith> CollideWithBufferArray;
			
			public void Execute(Entity e, int _, ref CustomCharacterController cc, ref Translation t, ref Rotation r, ref PhysicsVelocity pv, ref PhysicsCollider coll)
			{
				var cwBuffer = CollideWithBufferArray[e];

				var castInput = new ColliderCastInput
				{
					Collider    = coll.ColliderPtr,
					Direction   = new float3(0.0f, k_GroundCheckDistance, 0.0f),
					Orientation = r.Value,
					Position    = t.Value
				};
				var isGrounded = cwBuffer.CastCollider(castInput, out var closestHit);
				if (!isGrounded)
					pv.Linear.y = -9.0f;
				else
					pv.Linear.y = 0.0f;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job{CollideWithBufferArray = GetBufferFromEntity<CollideWith>()}.Schedule(this, inputDeps);
		}
	}
}