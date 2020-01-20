using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.Projectiles;
using Stormium.Core.Projectiles;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Playables;

namespace Components.GamePlay.Projectiles
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities.SpawnEvent))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class ProjectileDefaultExplosionSystem : JobGameBaseSystem
	{
		private BuildPhysicsWorld                                        m_BuildPhysicsWorld;
		private EntityArchetype                                          m_DamageEventArchetype;
		private OrderGroup.Simulation.DeleteEntities.CommandBufferSystem m_DeleteBarrier;
		private EntityQuery                                              m_ExplodedQuery;

		private EntityArchetype m_ImpulseEventArchetype;

		private EntityQuery                                             m_ProjectileQuery;
		private OrderGroup.Simulation.SpawnEntities.CommandBufferSystem m_SpawnBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ProjectileQuery = GetEntityQuery(new EntityQueryDesc {All = new ComponentType[] {typeof(ProjectileEndedTag)}});
			m_ExplodedQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(ProjectileEndedTag), typeof(ProjectileExplodedEndReason), typeof(Translation)},
				Any = new ComponentType[] {typeof(DistanceDamageFallOf), typeof(DistanceImpulseFallOf)}
			});

			m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
			m_DeleteBarrier     = World.GetOrCreateSystem<OrderGroup.Simulation.DeleteEntities.CommandBufferSystem>();
			m_SpawnBarrier      = World.GetOrCreateSystem<OrderGroup.Simulation.SpawnEntities.CommandBufferSystem>();

			m_ImpulseEventArchetype = World.GetOrCreateSystem<TargetImpulseEvent.Provider>().EntityArchetype;
			m_DamageEventArchetype  = World.GetOrCreateSystem<TargetDamageEvent.Provider>().EntityArchetype;
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new ProcessProjectileExplosion
			{
				Tick         = GetTick(true),
				PhysicsWorld = m_BuildPhysicsWorld.PhysicsWorld,

				EntityType        = GetArchetypeChunkEntityType(),
				TranslationType   = GetArchetypeChunkComponentType<Translation>(true),
				ExplosionType     = GetArchetypeChunkComponentType<ProjectileDefaultExplosion>(true),
				ImpulseFallOfType = GetArchetypeChunkBufferType<DistanceImpulseFallOf>(true),
				DamageFallOfType  = GetArchetypeChunkBufferType<DistanceDamageFallOf>(true),

				LivableHealthFromEntity = GetComponentDataFromEntity<LivableHealth>(true),
				MovableDescFromEntity   = GetComponentDataFromEntity<MovableDescription>(true),

				DamageFrameFromEntity = GetComponentDataFromEntity<DamageFrame>(true),

				RelativeMovableFromEntity = GetComponentDataFromEntity<Relative<MovableDescription>>(true),
				RelativeLivableFromEntity = GetComponentDataFromEntity<Relative<LivableDescription>>(true),

				Ecb                   = m_SpawnBarrier.CreateCommandBuffer().ToConcurrent(),
				ImpulseEventArchetype = m_ImpulseEventArchetype,
				DamageEventArchetype  = m_DamageEventArchetype
			}.Schedule(m_ExplodedQuery, JobHandle.CombineDependencies(inputDeps, m_BuildPhysicsWorld.FinalJobHandle));
			// We could have used EntityCommandBuffer.DestroyEntity(EntityQuery) but that mean we are destroying entities that we don't even know that exist.
			inputDeps = new DestroyProjectileJob
			{
				Ecb = m_DeleteBarrier.CreateCommandBuffer().ToConcurrent()
			}.Schedule(m_ProjectileQuery, inputDeps);

			m_DeleteBarrier.AddJobHandleForProducer(inputDeps);
			m_SpawnBarrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}

		//[BurstCompile]
		private struct ProcessProjectileExplosion : IJobChunk
		{
			public UTick Tick;

			[ReadOnly] public PhysicsWorld PhysicsWorld;

			[ReadOnly] public ArchetypeChunkEntityType                                EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<Translation>                TranslationType;
			[ReadOnly] public ArchetypeChunkComponentType<ProjectileDefaultExplosion> ExplosionType;
			[ReadOnly] public ArchetypeChunkBufferType<DistanceImpulseFallOf>         ImpulseFallOfType;
			[ReadOnly] public ArchetypeChunkBufferType<DistanceDamageFallOf>          DamageFallOfType;

			[ReadOnly] public ComponentDataFromEntity<MovableDescription> MovableDescFromEntity;
			[ReadOnly] public ComponentDataFromEntity<LivableHealth>      LivableHealthFromEntity;

			[ReadOnly] public ComponentDataFromEntity<DamageFrame> DamageFrameFromEntity;

			[ReadOnly] public ComponentDataFromEntity<Relative<MovableDescription>> RelativeMovableFromEntity;
			[ReadOnly] public ComponentDataFromEntity<Relative<LivableDescription>> RelativeLivableFromEntity;

			public EntityCommandBuffer.Concurrent Ecb;
			public EntityArchetype                ImpulseEventArchetype;
			public EntityArchetype                DamageEventArchetype;

			[NativeSetThreadIndex]
			private int m_ThreadIndex;

			private float GetDistance(float fraction, float maxDistance)
			{
				return fraction <= 0 || maxDistance <= 0
					? 1.0f
					: 1.0f - fraction / maxDistance;
			}

			private void DoImpulse(ref PointDistanceInput input, Entity source, in ProjectileDefaultExplosion explosion, DynamicBuffer<DistanceImpulseFallOf> buffer)
			{
				input.MaxDistance = explosion.BumpRadius;

				var rigidBodies = PhysicsWorld.Bodies;
				var rbCount     = rigidBodies.Length;
				for (var i = 0; i != rbCount; i++)
				{
					var rb = rigidBodies[i];
					if (!rb.HasCollider || rb.Entity == default || !MovableDescFromEntity.Exists(rb.Entity))
						continue;

					var cc         = new CustomCollide(rb);
					var collection = new CustomCollideCollection(ref cc);
					if (!collection.CalculateDistance(input, out var closestHit))
						continue;

					var fallOf     = buffer.GetFallOfResult<DistanceImpulseFallOf, float>(GetDistance(closestHit.Distance, input.MaxDistance));
					var horizontal = math.clamp(explosion.HorizontalImpulseMax * fallOf, explosion.HorizontalImpulseMin, explosion.HorizontalImpulseMax);
					var vertical   = math.clamp(explosion.VerticalImpulseMax * fallOf, explosion.HorizontalImpulseMin, explosion.HorizontalImpulseMax);
					var force      = -closestHit.SurfaceNormal;

					force.x *= horizontal;
					force.z *= horizontal;
					force.y *= vertical;
					if (RelativeMovableFromEntity.Exists(source) && RelativeMovableFromEntity[source].Target == rb.Entity)
						force *= explosion.SelfImpulseFactor;

					if (math.abs(force.y) < 0.1f)
						force.y += explosion.VerticalImpulseMin * 0.25f;

					var ev = Ecb.CreateEntity(m_ThreadIndex, ImpulseEventArchetype);
					Ecb.SetComponent(m_ThreadIndex, ev, new TargetImpulseEvent
					{
						Origin      = RelativeMovableFromEntity.Exists(source) ? RelativeMovableFromEntity[source].Target : source,
						Destination = rb.Entity,
						Force       = force,
						Momentum    = 1.0f,
						Position    = input.Position
					});
				}
			}

			private bool DoDamage(ref PointDistanceInput input, Entity source, in ProjectileDefaultExplosion explosion, DynamicBuffer<DistanceDamageFallOf> buffer)
			{
				input.MaxDistance = explosion.DamageRadius;

				var rigidBodies = PhysicsWorld.Bodies;
				var rbCount     = rigidBodies.Length;
				for (var i = 0; i != rbCount; i++)
				{
					var rb = rigidBodies[i];
					if (!rb.HasCollider || rb.Entity == default || (LivableHealthFromEntity.Exists(rb.Entity) && LivableHealthFromEntity[rb.Entity].IsDead))
						continue;

					var cc         = new CustomCollide(rb);
					var collection = new CustomCollideCollection(ref cc);
					if (!collection.CalculateDistance(input, out var closestHit))
						continue;

					var targetDamage = explosion.MaxDamage;
					var minDamage    = explosion.MinDamage;
					var maxDamage    = explosion.MaxDamage;
					if (DamageFrameFromEntity.TryGet(source, out var damageFrame))
					{
						targetDamage = damageFrame.Damage;
						maxDamage    = math.max(targetDamage, maxDamage);
						if (minDamage < 0)
							minDamage = 0;
					}

					var fallOf = buffer.GetFallOfResult<DistanceDamageFallOf, float>(GetDistance(closestHit.Distance, input.MaxDistance));
					var damage = math.clamp(targetDamage * fallOf, minDamage, maxDamage);

					var ev = Ecb.CreateEntity(m_ThreadIndex, DamageEventArchetype);
					Ecb.SetComponent(m_ThreadIndex, ev, new TargetDamageEvent
					{
						Origin      = RelativeLivableFromEntity.Exists(source) ? RelativeLivableFromEntity[source].Target : source,
						Destination = rb.Entity,
						Damage      = Mathf.FloorToInt(-damage)
					});
					Ecb.AddComponent(m_ThreadIndex, ev, new Translation {Value = closestHit.Position});

					Debug.Log($"Damage to {rb.Entity} -> {damage:F2} to {(int) damage}");
				}

				return false;
			}

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var input = new PointDistanceInput
				{
					MaxDistance = 0.0f,
					Filter      = CollisionFilter.Default
				};

				var entityArray        = chunk.GetNativeArray(EntityType);
				var translationArray   = chunk.GetNativeArray(TranslationType);
				var explosionArray     = chunk.GetNativeArray(ExplosionType);
				var impulseFallOfArray = chunk.GetBufferAccessor(ImpulseFallOfType);
				var damageFallOfArray  = chunk.GetBufferAccessor(DamageFallOfType);
				for (var ent = 0; ent < chunk.Count; ent++)
				{
					var translation = translationArray[ent];
					input.Position = translation.Value;

					if (chunk.Has(ImpulseFallOfType))
						DoImpulse(ref input, entityArray[ent], explosionArray[ent], impulseFallOfArray[ent]);

					if (chunk.Has(DamageFallOfType))
						DoDamage(ref input, entityArray[ent], explosionArray[ent], damageFallOfArray[ent]);
				}
			}
		}

		[BurstCompile]
		private struct DestroyProjectileJob : IJobChunk
		{
			public EntityCommandBuffer.Concurrent Ecb;

			[ReadOnly]
			public ArchetypeChunkEntityType EntityType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var entityArray = chunk.GetNativeArray(EntityType);
				for (var ent = 0; ent < chunk.Count; ent++)
					Ecb.DestroyEntity(chunkIndex, entityArray[ent]);
			}
		}
	}
}