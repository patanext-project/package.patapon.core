using Systems.GamePlay.CYari;
using package.stormiumteam.shared.ecs;
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
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Patapon.Mixed.GamePlay.Projectiles
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public unsafe class SpearProjectileSystem : AbsGameBaseSystem
	{
		private LazySystem<OrderGroup.Simulation.BeforeSpawnEntitiesCommandBuffer> m_Barrier;
		private LazySystem<BuildPhysicsWorld>                                      m_BuildPhysicsWorld;

		protected override void OnUpdate()
		{
			var tick          = ServerTick;
			var ageFromEntity = GetComponentDataFromEntity<ProjectileAgeTime>(true);
			var physicsWorld  = this.L(ref m_BuildPhysicsWorld).PhysicsWorld;
			var ecb           = this.L(ref m_Barrier).CreateCommandBuffer().ToConcurrent();

			var teamRelativeFromEntity = GetComponentDataFromEntity<Relative<TeamDescription>>(true);
			var enemiesFromTeam        = GetBufferFromEntity<TeamEnemies>(true);
			var seekEnemies            = new SeekEnemies(this);
			var impl                   = new BasicUnitAbilityImplementation(this);

			Entities
				.WithNone<ProjectileEndedTag>()
				.ForEach((Entity entity, int nativeThreadIndex, ref Translation translation, ref Velocity velocity, in Owner owner, in SpearProjectile projectile) =>
				{
					Entity* tryGetChain = stackalloc[] {entity, owner.Target};
					if (!teamRelativeFromEntity.TryGetChain(tryGetChain, 2, out var teamRelative))
						return;

					if (ageFromEntity.Exists(entity) && ageFromEntity[entity].EndMs < tick.Ms)
					{
						ecb.AddComponent<ProjectileEndedTag>(nativeThreadIndex, entity);
						ecb.AddComponent<ProjectileOutOfTimeEndReason>(nativeThreadIndex, entity);
						return;
					}

					var     blobCollider = SphereCollider.Create(new SphereGeometry {Radius = projectile.DetectionRadius}, CollisionFilter.Default);
					ref var collider     = ref blobCollider.Value;

					var input = new ColliderCastInput
					{
						Collider = (Collider*) UnsafeUtility.AddressOf(ref collider)
					};
					var end = ProjectileUtility.Project(translation.Value, ref velocity.Value, tick.Delta, projectile.Gravity);

					input.Start = translation.Value;
					input.End   = end;
					Debug.DrawLine(input.Start, input.End, Color.green, 0.1f);

					var enemyBuffer = enemiesFromTeam[teamRelative.Target];
					var enemies     = new NativeList<Entity>(Allocator.Temp);
					seekEnemies.GetAllEnemies(ref enemies, enemyBuffer);

					if (Cast(seekEnemies, impl, enemies, teamRelativeFromEntity, input, out var hit)
						/*|| EnvironmentalCast(seekEnemies, entity, teamRelative.Target, teamRelativeFromEntity, physicsWorld, input, out hit) != -1*/)
					{
						ecb.AddComponent<ProjectileEndedTag>(nativeThreadIndex, entity);
						ecb.AddComponent(nativeThreadIndex, entity, new ProjectileExplodedEndReason {normal = hit.SurfaceNormal});

						end = hit.Position;
					}
					else if (end.y < 0)
					{
						ecb.AddComponent<ProjectileEndedTag>(nativeThreadIndex, entity);
						ecb.AddComponent(nativeThreadIndex, entity, new ProjectileExplodedEndReason {normal = new float3(0, 1, 0)});

						end.y = 0;
					}

					translation.Value = end;

					blobCollider.Dispose();
				})
				//.WithReadOnly(physicsWorld)
				.WithReadOnly(teamRelativeFromEntity)
				.WithReadOnly(enemiesFromTeam)
				.WithReadOnly(ageFromEntity)
				.Schedule();
		}

		private static bool Cast(SeekEnemies          seekEnemies, BasicUnitAbilityImplementation impl, NativeList<Entity> enemies, ComponentDataFromEntity<Relative<TeamDescription>> teamRelativeCdfe,
		                         in ColliderCastInput input,       out ColliderCastHit            hit)
		{
			hit = default;

			var minFriction = float.MaxValue;
			var rigidBodies = new NativeList<RigidBody>(enemies.Length, Allocator.Temp);
			for (var ent = 0; ent != enemies.Length; ent++)
			{
				var enemy = enemies[ent];
				if (!seekEnemies.CanHitTarget(enemy))
					continue;

				rigidBodies.Clear();
				CreateRigidBody.Execute(ref rigidBodies, seekEnemies.HitShapeContainer[enemy].AsNativeArray(),
					enemy,
					impl.LocalToWorld, impl.Translation, seekEnemies.Collider);
				for (var i = 0; i != rigidBodies.Length; i++)
				{
					var cc = new CustomCollide(rigidBodies[i]) {WorldFromMotion = {pos = {z = 0}}};
					if (!new CustomCollideCollection(ref cc).CastCollider(input, out var closestHit) && closestHit.Fraction < minFriction)
						continue;

					minFriction = closestHit.Fraction;
					hit         = closestHit;
				}
			}

			return minFriction != float.MaxValue;
		}

		private static int EnvironmentalCast(SeekEnemies     seekEnemies,  Entity               ent,   Entity              team, ComponentDataFromEntity<Relative<TeamDescription>> teamRelativeCdfe,
		                                     in PhysicsWorld physicsWorld, in ColliderCastInput input, out ColliderCastHit hit)
		{
			hit = default;

			var rigidBodies       = physicsWorld.Bodies;
			var rbCount           = rigidBodies.Length;
			var minFriction       = float.MaxValue;
			var selectedRigidBody = -1;
			for (var i = 0; i < rbCount; i++)
			{
				var rb = rigidBodies[i];
				if (!rb.HasCollider || rb.Entity == ent || teamRelativeCdfe.Exists(rb.Entity) && teamRelativeCdfe[rb.Entity].Target == team)
					continue;
				if (!seekEnemies.CanHitTargetIgnoreHitShape(rb.Entity))
					continue;

				rb.WorldFromBody.rot = math.normalizesafe(rb.WorldFromBody.rot);

				var cc         = new CustomCollide(rb);
				var collection = new CustomCollideCollection(ref cc);
				if (collection.CastCollider(input, out var closestHit) && closestHit.Fraction < minFriction)
				{
					minFriction       = closestHit.Fraction;
					selectedRigidBody = i;

					hit = closestHit;
				}
			}

			return selectedRigidBody;
		}
	}
}