using package.stormiumteam.shared.ecs;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Patapon.Mixed.GamePlay
{
	public struct SeekEnemies
	{
		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public BufferFromEntity<TeamEntityContainer> Entities;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public BufferFromEntity<HitShapeContainer> HitShapeContainer;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public ComponentDataFromEntity<LivableHealth> LivableHealth;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public ComponentDataFromEntity<LocalToWorld> LocalToWorld;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public ComponentDataFromEntity<Translation> Translation;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public ComponentDataFromEntity<PhysicsCollider> Collider;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public ComponentDataFromEntity<UnitEnemySeekingState> SeekingState;


		public SeekEnemies(ComponentSystemBase systemBase)
		{
			Entities          = systemBase.GetBufferFromEntity<TeamEntityContainer>(true);
			HitShapeContainer = systemBase.GetBufferFromEntity<HitShapeContainer>(true);
			LivableHealth     = systemBase.GetComponentDataFromEntity<LivableHealth>(true);
			LocalToWorld      = systemBase.GetComponentDataFromEntity<LocalToWorld>(true);
			Translation       = systemBase.GetComponentDataFromEntity<Translation>(true);
			Collider          = systemBase.GetComponentDataFromEntity<PhysicsCollider>(true);
			SeekingState      = systemBase.GetComponentDataFromEntity<UnitEnemySeekingState>(true);
		}

		public bool CanHitTarget(Entity target)
		{
			return (!LivableHealth.TryGet(target, out var enemyHealth) || !enemyHealth.IsDead)
			       && HitShapeContainer.Exists(target);
		}

		public bool CanHitTargetIgnoreHitShape(Entity target)
		{
			return !LivableHealth.TryGet(target, out var enemyHealth) || !enemyHealth.IsDead;
		}

		public unsafe void SeekNearest(float3 from, float seekRange, NativeList<Entity> enemies, out Entity nearestEnemy, out float3 target, out float targetDistance)
		{
			nearestEnemy   = Entity.Null;
			target         = float3.zero;
			targetDistance = default;

			var shortestDistance = float.MaxValue;
			for (var ent = 0; ent != enemies.Length; ent++)
			{
				var entity = enemies[ent];
				if (LivableHealth.Exists(entity) && LivableHealth[entity].IsDead)
					continue;
				if (!HitShapeContainer.Exists(entity))
					continue;
				if (!LocalToWorld.Exists(entity))
					continue;

				var parentTransform = LocalToWorld[entity];
				var distance        = math.distance(from.x, parentTransform.Position.x);
				var hitShapes       = HitShapeContainer[entity];
				if (hitShapes.Length > 0)
				{
					var direction = (int) math.sign(parentTransform.Position.x - from.x);
					var hsArray   = hitShapes.AsNativeArray();
					for (var hs = 0; hs != hsArray.Length; hs++)
					{
						var hitShape    = hsArray[hs];
						var translation = Translation[hitShape.Value].Value;
						if (!LocalToWorld.TryGet(hitShape.Value, out var ltw))
						{
							ltw = new LocalToWorld {Value = new float4x4(quaternion.identity, translation)};
						}
						else
						{
							// translation is always updated after ltw!
							ltw.Value = new float4x4(ltw.Rotation, translation);
						}

						RigidBody rigidBody = default;
						rigidBody.Entity        = hitShape.Value;
						rigidBody.Collider      = Collider[hitShape.Value].Value;
						rigidBody.WorldFromBody = new RigidTransform(ltw.Value);

						if (hitShape.AttachedToParent)
						{
							rigidBody.WorldFromBody.pos += parentTransform.Position;
						}

						var aabb = rigidBody.CalculateAabb();
						if (direction > 0)
							distance = math.distance(from.x, aabb.Min.x);
						else
							distance = math.distance(from.x, aabb.Max.x);

						if (distance > seekRange)
							continue;
						if (distance > shortestDistance)
							continue;

						target           = parentTransform.Position;
						targetDistance   = distance;
						shortestDistance = distance;
						nearestEnemy     = entity;
					}

					continue;
				}

				if (distance > seekRange)
					continue;

				if (distance > shortestDistance)
					continue;

				target           = parentTransform.Position;
				targetDistance   = distance;
				shortestDistance = distance;
				nearestEnemy     = entity;
			}
		}

		public void GetAllEnemies(ref NativeList<Entity> output, DynamicBuffer<TeamEnemies> enemies)
		{
			for (int team = 0, teamLength = enemies.Length; team != teamLength; team++)
			{
				var entities = Entities[enemies[team].Target];
				output.AddRange(entities.Reinterpret<Entity>().AsNativeArray());
			}
		}

		public void GetAllEnemies(ref NativeList<Entity> output, DynamicBuffer<TeamEnemies> enemies, RigidBody rigidBody, NativeList<RigidBody> enemiesRigidBodies)
		{
			for (var team = 0; team != enemies.Length; team++)
			{
				var entities = Entities[enemies[team].Target];
				for (var ent = 0; ent != entities.Length; ent++)
				{
					var entity = entities[ent];
				}
			}
		}
	}
}