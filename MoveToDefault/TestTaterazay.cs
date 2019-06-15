namespace Patapon4TLB.Default
{
	// todo
	/*public class TestTaterazay : ComponentSystem
	{
		protected override void OnStartRunning()
		{
			var existingColliders = new NativeList<Entity>(Allocator.Temp);
			ForEach((Entity entity, ref PhysicsCollider coll) => { existingColliders.Add(entity); });

			Entity characterEntity, colliderEntity, movableEntity;

			var collisionFilter = CollisionFilter.Default;
			collisionFilter.GroupIndex = -493; // todo: temp id, it should generate one instead.


			characterEntity = EntityManager.CreateEntity
			(
				// headers
				typeof(LivableDescription),
				typeof(CharacterDescription),

				// characters things...
				typeof(TaterazayKitDescription),
				typeof(TaterazayKitBehaviorData),
				typeof(UnitDirection),
				typeof(UnitBaseSettings),
				typeof(RhythmActionController),
				typeof(ActionContainer),
				typeof(HealthContainer),

				typeof(OwnerChild)
			);
			{
				EntityManager.SetComponentData(characterEntity, new UnitDirection
				{
					Value = 1
				});
				EntityManager.SetComponentData(characterEntity, new UnitBaseSettings
				{
					BaseSpeed = 6
				});
			}

			colliderEntity = EntityManager.CreateEntity
			(
				typeof(ColliderDescription),

				typeof(Translation),
				typeof(Rotation),
				typeof(LocalToWorld),

				// Add static collider
				typeof(PhysicsCollider),

				typeof(GenerateEntitySnapshot)
			);
			{
				var bxCollider = BoxCollider.Create(new float3(0, 1, 0), quaternion.identity, new float3(1, 2, 1), 0.01f, collisionFilter);

				EntityManager.SetComponentData(colliderEntity, new PhysicsCollider {Value = bxCollider});
			}

			movableEntity = EntityManager.CreateEntity
			(
				// headers
				typeof(MovableDescription),

				// default components for movables...
				typeof(Translation),
				typeof(Rotation),
				typeof(LocalToWorld),

				// default components for physics movables...
				typeof(PhysicsCollider),
				typeof(PhysicsMass),
				typeof(Velocity),
				typeof(PhysicsGravityFactor),

				typeof(CustomCollide),

				typeof(GenerateEntitySnapshot)
			);
			{
				var cpCollider = CapsuleCollider.Create(float3.zero, new float3(0, 2, 0), 0.5f, collisionFilter);

				EntityManager.SetComponentData(movableEntity, new Translation {Value = new float3(-2, 4, 0)});
				EntityManager.SetComponentData(movableEntity, new Rotation {Value    = quaternion.identity});

				EntityManager.SetComponentData(movableEntity, new PhysicsCollider
				{
					Value = cpCollider
				});
				EntityManager.SetComponentData(movableEntity, PhysicsMass.CreateKinematic(cpCollider.Value.MassProperties));
				EntityManager.SetComponentData(movableEntity, new PhysicsGravityFactor
				{
					Value = 0f // kinematic body are not affected by normal gravity...
				});

				var cwBuffer = EntityManager.GetBuffer<CustomCollide>(movableEntity);
				for (var i = 0; i != existingColliders.Length; i++)
					cwBuffer.Add(new CustomCollide {Target = existingColliders[i]});
			}

			var childBuffer = EntityManager.GetBuffer<OwnerChild>(characterEntity);
			childBuffer.Add(OwnerChild.Create<ColliderDescription>(colliderEntity));
			childBuffer.Add(OwnerChild.Create<MovableDescription>(movableEntity));

			var marchAction = EntityManager.CreateEntity
			(
				typeof(OwnerState<LivableDescription>),
				typeof(ActionDescription),
				typeof(MarchDefenseAbility.Settings),
				typeof(MarchDefenseAbility.PredictedState),
				typeof(GenerateEntitySnapshot)
			);

			EntityManager.ReplaceOwnerData(movableEntity, characterEntity);
			EntityManager.ReplaceOwnerData(colliderEntity, characterEntity);
			EntityManager.ReplaceOwnerData(marchAction, characterEntity);

			EntityManager.AddComponentData(movableEntity, new DestroyChainReaction(characterEntity));
			EntityManager.AddComponentData(colliderEntity, new DestroyChainReaction(characterEntity));
			EntityManager.AddComponentData(marchAction, new DestroyChainReaction(characterEntity));

			// create some health data...
			//var defaultHealthAsset = EntityManager.CreateEntity(typeof(AssetDescription), typeof(HealthAssetDescription), typeof());

			var defaultHealthInstanceProvider = World.GetExistingSystem<DefaultHealthData.InstanceProvider>();
			using (var outputEntities = new NativeList<Entity>(Allocator.TempJob))
			{
				defaultHealthInstanceProvider.SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance {value = 100, max = 100, owner = characterEntity}, outputEntities);
			}
		}

		protected override void OnUpdate()
		{
			ForEach((Entity entity, ref Translation t, ref Rotation r, ref OwnerState<MovableDescription> movableOwner) =>
			{
				var translation = EntityManager.GetComponentData<Translation>(movableOwner.Target);
				var rotation    = EntityManager.GetComponentData<Rotation>(movableOwner.Target);

				t.Value = translation.Value;
				r.Value = rotation.Value;
			});
		}
	}*/
}