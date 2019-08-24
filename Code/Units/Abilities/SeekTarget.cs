using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Patapon4TLB.Default
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

		public SeekEnemies(JobComponentSystem systemBase)
		{
			EntitiesFromTeam = systemBase.GetBufferFromEntity<TeamEntityContainer>(true);
			HitShapeContainerFromEntity = systemBase.GetBufferFromEntity<HitShapeContainer>(true);
			LivableHealthFromEntity = systemBase.GetComponentDataFromEntity<LivableHealth>(true);
			LocalToWorldFromEntity = systemBase.GetComponentDataFromEntity<LocalToWorld>(true);
		}
		
		public void Execute(float3 from, float seekRange, DynamicBuffer<TeamEnemies> enemies, out Entity nearestEnemy, out float3 target, out float targetDistance)
		{
			nearestEnemy   = Entity.Null;
			target         = float3.zero;
			targetDistance = default;

			var shortestDistance = float.MaxValue;
			for (var team = 0; team != enemies.Length; team++)
			{
				var entities = EntitiesFromTeam[enemies[team].Target];
				for (var ent = 0; ent != entities.Length; ent++)
				{
					var entity = entities[ent].Value;
					if (LivableHealthFromEntity.Exists(entity) && LivableHealthFromEntity[entity].IsDead)
						continue;
					if (!HitShapeContainerFromEntity.Exists(entity))
						continue;
					if (!LocalToWorldFromEntity.Exists(entity))
						continue;
							
					var transform = LocalToWorldFromEntity[entity];

					var distance = math.distance(from.x, transform.Position.x);
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
		}

		public NativeArray<Entity> GetEntitiesInRange(float3 from, float range, DynamicBuffer<TeamEnemies> enemies)
		{
			var list = new NativeList<Entity>(enemies.Length * 8, Allocator.Temp);
			for (var team = 0; team != enemies.Length; team++)
			{
				var entities = EntitiesFromTeam[enemies[team].Target];
				for (var ent = 0; ent != entities.Length; ent++)
				{
					var entity = entities[ent].Value;
					if (LivableHealthFromEntity.Exists(entity) && LivableHealthFromEntity[entity].IsDead)
						continue;
					if (!HitShapeContainerFromEntity.Exists(entity))
						continue;
					if (LocalToWorldFromEntity.Exists(entity))
						continue;
					
					var transform = LocalToWorldFromEntity[entity];
					if (math.distance(transform.Position.x, from.x) > range)
						continue;
					
					list.Add(entity);
				}
			}

			return list;
		}
	}
}