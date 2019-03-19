using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Patapon4TLB.Default
{
	public class TestTaterazay : ComponentSystem
	{
		protected override void OnStartRunning()
		{
			var cpCollider = CapsuleCollider.Create(float3.zero, new float3(0, 2, 0), 0.5f);
			var characterEntity = EntityManager.CreateEntity
			(
				// default components for movables...
				typeof(Translation),
				typeof(Rotation),

				// default components for physics movables...
				typeof(PhysicsCollider),
				typeof(PhysicsMass),
				typeof(PhysicsVelocity),
				typeof(PhysicsGravityFactor),

				// default components for characters...
				typeof(LivableDescription),
				typeof(ActionContainer),

				// kit custom components...
				typeof(TaterazayKitDescription),
				typeof(TaterazayKitBehaviorData),
				typeof(UnitDirection),
				typeof(UnitBaseSettings),
				typeof(RhythmActionController)
			);

			// ...
			EntityManager.SetComponentData(characterEntity, new Translation
			{
				Value = new float3(0, 2, 0)
			});
			EntityManager.SetComponentData(characterEntity, new Rotation
			{
				Value = quaternion.identity
			});
			// ...
			EntityManager.SetComponentData(characterEntity, new PhysicsCollider
			{
				Value = cpCollider
			});
			EntityManager.SetComponentData(characterEntity, PhysicsMass.CreateKinematic(cpCollider.Value.MassProperties));
			EntityManager.SetComponentData(characterEntity, new PhysicsVelocity
			{
				Linear = new float3(1, 0, 0), Angular = float3.zero
			});
			EntityManager.SetComponentData(characterEntity, new PhysicsGravityFactor
			{
				Value = 0f // kinematic body are not affected by normal gravity...
			});
			// ...
			EntityManager.SetComponentData(characterEntity, new UnitDirection
			{
				Value = 1
			});
			EntityManager.SetComponentData(characterEntity, new UnitBaseSettings
			{
				BaseSpeed = 6
			});

			var marchAction = EntityManager.CreateEntity
			(
				typeof(OwnerState<LivableDescription>),
				typeof(ActionDescription),
				typeof(TaterazayKitMarchAction.Settings)
			);

			EntityManager.ReplaceOwnerData(marchAction, characterEntity);
		}

		protected override void OnUpdate()
		{

		}
	}
}