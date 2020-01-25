using package.stormiumteam.shared.ecs;
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
		public BufferFromEntity<TeamEntityContainer> EntitiesFromTeam;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public BufferFromEntity<HitShapeContainer> HitShapeContainerFromEntity;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public ComponentDataFromEntity<LivableHealth> LivableHealthFromEntity;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public ComponentDataFromEntity<LocalToWorld> LocalToWorldFromEntity;
		
		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public ComponentDataFromEntity<Translation> TranslationFromEntity;

		[NativeDisableContainerSafetyRestriction, ReadOnly]
		public ComponentDataFromEntity<PhysicsCollider> ColliderFromEntity;

		public SeekEnemies(JobComponentSystem systemBase)
		{
			EntitiesFromTeam            = systemBase.GetBufferFromEntity<TeamEntityContainer>(true);
			HitShapeContainerFromEntity = systemBase.GetBufferFromEntity<HitShapeContainer>(true);
			LivableHealthFromEntity     = systemBase.GetComponentDataFromEntity<LivableHealth>(true);
			LocalToWorldFromEntity      = systemBase.GetComponentDataFromEntity<LocalToWorld>(true);
			TranslationFromEntity       = systemBase.GetComponentDataFromEntity<Translation>(true);
			ColliderFromEntity          = systemBase.GetComponentDataFromEntity<PhysicsCollider>(true);
		}

		public bool CanHitTarget(Entity target)
		{
			return (!LivableHealthFromEntity.TryGet(target, out var enemyHealth) || !enemyHealth.IsDead)
			       && HitShapeContainerFromEntity.Exists(target);
		}
		
		public bool CanHitTargetIgnoreHitShape(Entity target)
		{
			return !LivableHealthFromEntity.TryGet(target, out var enemyHealth) || !enemyHealth.IsDead;
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
				if (LivableHealthFromEntity.Exists(entity) && LivableHealthFromEntity[entity].IsDead)
					continue;
				if (!HitShapeContainerFromEntity.Exists(entity))
					continue;
				if (!LocalToWorldFromEntity.Exists(entity))
					continue;

				var transform = LocalToWorldFromEntity[entity];
				var distance = math.distance(from.x, transform.Position.x);
				var hitShapes = HitShapeContainerFromEntity[entity];
				if (hitShapes.Length > 0 && false)
				{
					var direction = (int) math.sign(transform.Position.x - from.x);
					var hsArray   = hitShapes.AsNativeArray();
					for (var hs = 0; hs != hsArray.Length; hs++)
					{
						var hitShape    = hsArray[hs];
						var translation = TranslationFromEntity[hitShape.Value].Value;
						if (!LocalToWorldFromEntity.TryGet(hitShape.Value, out var ltw))
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
						rigidBody.Collider      = ColliderFromEntity[hitShape.Value].ColliderPtr;
						rigidBody.WorldFromBody = new RigidTransform(ltw.Value);

						if (hitShape.AttachedToParent)
						{
							var hasTranslation = TranslationFromEntity.TryGet(entity, out var ownerTranslation);
							if (!LocalToWorldFromEntity.TryGet(entity, out ltw))
							{
								ltw = new LocalToWorld {Value = new float4x4(quaternion.identity, ownerTranslation.Value)};
							}
							else if (hasTranslation)
							{
								ltw.Value = new float4x4(ltw.Rotation, ownerTranslation.Value);
							}

							rigidBody.WorldFromBody.pos += ltw.Position;
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

						target           = transform.Position;
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
				
				target           = transform.Position;
				targetDistance   = distance;
				shortestDistance = distance;
				nearestEnemy     = entity;
			}
		}

		public void GetAllEnemies(ref NativeList<Entity> output, DynamicBuffer<TeamEnemies> enemies)
		{
			for (int team = 0, teamLength = enemies.Length; team != teamLength; team++)
			{
				var entities = EntitiesFromTeam[enemies[team].Target];
				output.AddRange(entities.Reinterpret<Entity>().AsNativeArray());
			}
		}

		public void GetAllEnemies(ref NativeList<Entity> output, DynamicBuffer<TeamEnemies> enemies, RigidBody rigidBody, NativeList<RigidBody> enemiesRigidBodies)
		{
			for (var team = 0; team != enemies.Length; team++)
			{
				var entities = EntitiesFromTeam[enemies[team].Target];
				for (var ent = 0; ent != entities.Length; ent++)
				{
					var entity = entities[ent];
				}
			}
		}
	}
}