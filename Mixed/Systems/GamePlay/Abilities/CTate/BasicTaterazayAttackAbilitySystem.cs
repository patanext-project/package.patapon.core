using Systems.GamePlay.CYari;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities.CTate;
using Patapon.Mixed.Units;
using Patapon.Mixed.Utilities;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Systems.GamePlay.CTate
{
	public unsafe class BasicTaterazayAttackAbilitySystem : BaseAbilitySystem
	{
		private TargetDamageEvent.Provider m_DamageEventProvider;
		private JobPhysicsQuery            m_HitQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_HitQuery = new JobPhysicsQuery(() => SphereCollider.Create(new SphereGeometry
			{
				Center = float3.zero,
				Radius = 2f
			}));
			m_DamageEventProvider = World.GetOrCreateSystem<TargetDamageEvent.Provider>();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			m_HitQuery.Dispose();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var tick                      = ServerTick;
			var enemiesFromTeam           = GetBufferFromEntity<TeamEnemies>(true);
			var physicsColliderFromEntity = GetComponentDataFromEntity<PhysicsCollider>(true);
			var impl                      = new BasicUnitAbilityImplementation(this);
			var seekEnemies               = new SeekEnemies(this);
			var seekingStateFromEntity    = GetComponentDataFromEntity<UnitEnemySeekingState>(true);

			var colliderQuery          = m_HitQuery;
			var relativeTeamFromEntity = GetComponentDataFromEntity<Relative<TeamDescription>>(true);

			var damageEvArchetype = m_DamageEventProvider.EntityArchetype;
			var ecb = m_DamageEventProvider.CreateEntityCommandBuffer();

			Entities
				.ForEach((Entity entity, int nativeThreadIndex, ref RhythmAbilityState state, ref BasicTaterazayAttackAbility ability, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					var seekingState = seekingStateFromEntity[owner.Target];
					var playState    = impl.UnitPlayStateFromEntity[owner.Target];
					var unitPosition = impl.TranslationFromEntity[owner.Target].Value;

					var velocityUpdater   = impl.VelocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);
					var controllerUpdater = impl.ControllerFromEntity.GetUpdater(owner.Target).Out(out var controller);

					var attackStartTick = UTick.CopyDelta(tick, ability.AttackStartTick);

					if (state.IsStillChaining && !state.IsActive)
					{
						controller.ControlOverVelocity.x = true;
						velocity.Value.x                 = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 50 * tick.Delta);
					}

					ability.NextAttackDelay -= tick.Delta;
					if (ability.AttackStartTick > 0)
					{
						controller.ControlOverVelocity.x = true;

						// slash!
						if (tick >= UTick.AddMs(attackStartTick, BasicTaterazayAttackAbility.DelaySlashMs) && !ability.HasSlashed)
						{
							ability.HasSlashed = true;

							var   damage      = playState.Attack;
							float damageFever = damage;
							if (state.Combo.IsFever)
							{
								damageFever *= 1.2f;
								if (state.Combo.IsPerfect)
									damageFever *= 1.2f;

								damage += (int) damageFever - damage;
							}

							var unitDirection = impl.UnitDirectionFromEntity[owner.Target];
							var distanceInput = CreateDistanceFlatInput.ColliderWithOffset(colliderQuery.Ptr, unitPosition.xy, new float2(unitDirection.Value, 1));
							var allEnemies    = new NativeList<Entity>(Allocator.Temp);

							var tryGetChain = stackalloc[] {entity, owner.Target};
							if (!relativeTeamFromEntity.TryGetChain(tryGetChain, 2, out var relativeTeam))
								return;

							var teamEnemies = enemiesFromTeam[relativeTeam.Target];

							seekEnemies.GetAllEnemies(ref allEnemies, teamEnemies);

							var rigidBodies = new NativeList<RigidBody>(allEnemies.Length, Allocator.Temp);
							for (var ent = 0; ent != allEnemies.Length; ent++)
							{
								var enemy = allEnemies[ent];
								if (!seekEnemies.CanHitTarget(enemy))
									continue;

								rigidBodies.Clear();
								CreateRigidBody.Execute(ref rigidBodies, seekEnemies.HitShapeContainerFromEntity[enemy].AsNativeArray(),
									enemy,
									impl.LocalToWorldFromEntity, impl.TranslationFromEntity, physicsColliderFromEntity);
								for (var i = 0; i != rigidBodies.Length; i++)
								{
									var cc = new CustomCollide(rigidBodies[i]) {WorldFromMotion = {pos = {z = 0}}};
									if (!new CustomCollideCollection(ref cc).CalculateDistance(distanceInput, out var closestHit))
										continue;

									var evEnt = ecb.CreateEntity(damageEvArchetype);
									ecb.SetComponent(evEnt, new TargetDamageEvent
									{
										Origin = owner.Target, Destination = enemy, Damage = -damage
									});
									ecb.AddComponent(evEnt, new Translation {Value = closestHit.Position});
									break;
								}
							}
						}

						// stop moving
						if (ability.HasSlashed)
							velocity.Value.x = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 30 * tick.Delta);

						// stop attacking once the animation is done
						if (tick >= UTick.AddMs(attackStartTick, 500))
							ability.AttackStartTick = 0;
					}

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);

					// if inactive or no enemy are present, continue...
					if (!state.IsActive || seekingState.Enemy == default)
						return;

					var statistics = impl.UnitSettingsFromEntity[owner.Target];

					controller.ControlOverVelocity.x = true;

					if (state.Combo.IsFever)
					{
						playState.MovementAttackSpeed *= 1.8f;
						if (state.Combo.IsPerfect)
							playState.MovementAttackSpeed *= 1.2f;
					}

					// if all conditions are ok, start attacking.
					if (seekingState.SelfDistance <= statistics.AttackMeleeRange && ability.NextAttackDelay <= 0.0f && ability.AttackStartTick <= 0)
					{
						var atkSpeed = playState.AttackSpeed;
						if (state.Combo.IsFever && state.Combo.IsPerfect)
							atkSpeed *= 0.75f;

						ability.NextAttackDelay = atkSpeed;
						ability.AttackStartTick = tick.AsUInt;
						ability.HasSlashed      = false;
					}
					else if (tick >= UTick.AddMs(attackStartTick, BasicTaterazayAttackAbility.DelaySlashMs))
					{
						var direction = math.sign(seekingState.SelfPosition.x - unitPosition.x);
						velocity.Value.x = math.lerp(velocity.Value.x, playState.MovementAttackSpeed * direction, playState.GetAcceleration() * 50 * tick.Delta);
					}

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);
				})
				.WithReadOnly(enemiesFromTeam)
				.WithReadOnly(physicsColliderFromEntity)
				.WithReadOnly(seekingStateFromEntity)
				.WithReadOnly(relativeTeamFromEntity)
				.Run();

			return default;
		}
	}
}