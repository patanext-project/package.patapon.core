using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities.CTate;
using Patapon.Mixed.GamePlay.Abilities.CYari;
using Patapon.Mixed.GamePlay.Physics;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Systems.GamePlay.CYari
{
	[UpdateInGroup(typeof(RhythmAbilitySystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public unsafe class BasicYaridaDefendAbilitySystem : JobGameBaseSystem
	{
		private SpearProjectile.Provider m_ProjectileProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ProjectileProvider = World.GetOrCreateSystem<SpearProjectile.Provider>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var tick                   = ServerTick;
			var impl                   = new BasicUnitAbilityImplementation(this);
			var seekEnemies            = new SeekEnemies(this);
			var seekingStateFromEntity = GetComponentDataFromEntity<UnitEnemySeekingState>(true);

			var relativeTeamFromEntity = GetComponentDataFromEntity<Relative<TeamDescription>>(true);

			var queueWriter = m_ProjectileProvider.GetEntityDelayedStream()
			                                      .AsParallelWriter();

			inputDeps =
				Entities
					.ForEach((Entity entity, int nativeThreadIndex, ref RhythmAbilityState state, ref BasicYaridaDefendAbility ability, in Owner owner) =>
					{
						Entity* tryGetChain = stackalloc[] {entity, owner.Target};
						if (!relativeTeamFromEntity.TryGetChain(tryGetChain, 2, out var relativeTeam))
							return;

						var seekingState = seekingStateFromEntity[owner.Target];
						var statistics   = impl.UnitSettingsFromEntity[owner.Target];
						var playState    = impl.UnitPlayStateFromEntity[owner.Target];
						var unitPosition = impl.TranslationFromEntity[owner.Target].Value;
						var direction    = impl.UnitDirectionFromEntity[owner.Target].Value;

						var velocityUpdater   = impl.VelocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);
						var controllerUpdater = impl.ControllerFromEntity.GetUpdater(owner.Target).Out(out var controller);

						var attackStartTick = UTick.CopyDelta(tick, ability.AttackStartTick);

						if (state.IsStillChaining)
						{
							if (!state.IsActive)
							{
								controller.ControlOverVelocity.x = true;
								velocity.Value.x                 = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 50 * tick.Delta);	
							}
							
							float defense = playState.Defense * 0.25f;
							
							playState.ReceiveDamagePercentage *= 0.75f;
							if (state.Combo.IsFever)
							{
								playState.ReceiveDamagePercentage *= 0.8f;
								defense *= 1.2f;
								if (state.Combo.IsPerfect)
								{
									playState.ReceiveDamagePercentage *= 0.8f;
									defense *= 1.2f;
								}
							}
							
							playState.Defense += (int) defense;
						}

						ability.NextAttackDelay -= tick.Delta;

						var throwOffset = new float3 {x = direction, y = 1.75f};
						var gravity     = new float3 {y = -10};
						if (ability.AttackStartTick > 0)
						{
							if (tick >= UTick.AddMs(attackStartTick, BasicYaridaDefendAbility.DelayThrowMs) && !ability.HasThrown)
							{
								int   damage      = playState.Attack / 2;
								float damageFever = damage;
								if (state.Combo.IsFever)
								{
									damageFever *= 1.2f;
									if (state.Combo.IsPerfect)
										damageFever *= 1.2f;

									damage += (int) damageFever - damage;
								}

								queueWriter.Enqueue(new SpearProjectile.Create
								{
									Owner       = owner.Target,
									Position    = unitPosition + throwOffset,
									Velocity    = new float3(ability.ThrowVec, 0),
									StartDamage = damage,
									Gravity     = gravity
								});

								ability.HasThrown = true;
							}
							else if (!ability.HasThrown)
								controller.ControlOverVelocity.x = true;

							// stop moving
							if (ability.HasThrown)
								velocity.Value.x = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 25 * tick.Delta);

							// stop attacking once the animation is done
							if (tick >= UTick.AddMs(attackStartTick, 500))
								ability.AttackStartTick = 0;
						}

						velocityUpdater.CompareAndUpdate(velocity);
						controllerUpdater.CompareAndUpdate(controller);

						if (!state.IsActive || seekingState.Enemy == default || seekingState.Distance > statistics.AttackSeekRange * 0.7f)
							return;

						if (state.Combo.IsFever)
						{
							playState.MovementAttackSpeed *= 1.8f;
							if (state.Combo.IsPerfect)
								playState.MovementAttackSpeed *= 1.2f;
						}

						var targetPosition     = impl.LocalToWorldFromEntity[seekingState.Enemy].Position;
						var throwDeltaPosition = PredictTrajectory.Simple(throwOffset, new float3(ability.ThrowVec, 0), new float3(0, -22, 0));
						targetPosition.x -= throwDeltaPosition.x;

						float distanceMercy = 1.8f;
						if ((targetPosition.x - unitPosition.x * direction) < distanceMercy && ability.NextAttackDelay <= 0 && ability.AttackStartTick <= 0)
						{
							var atkSpeed = playState.AttackSpeed;
							if (state.Combo.IsFever && state.Combo.IsPerfect)
								atkSpeed *= 0.75f;

							ability.NextAttackDelay = atkSpeed;
							ability.AttackStartTick = tick.AsUInt;
							ability.HasThrown       = false;

							var speed   = math.lerp(math.abs(velocity.Value.x), playState.MovementAttackSpeed, playState.GetAcceleration() * 5 * tick.Delta);
							var newPosX = Mathf.MoveTowards(unitPosition.x, targetPosition.x, speed * tick.Delta);

							var targetVelocityX = (newPosX - unitPosition.x) / tick.Delta;
							velocity.Value.x = targetVelocityX;
						}
						else if (tick >= UTick.AddMs(attackStartTick, BasicYaridaDefendAbility.DelayThrowMs))
						{
							var speed   = math.lerp(math.abs(velocity.Value.x), playState.MovementAttackSpeed, playState.GetAcceleration() * 20 * tick.Delta);
							var newPosX = Mathf.MoveTowards(unitPosition.x, targetPosition.x, speed * tick.Delta);

							var targetVelocityX = (newPosX - unitPosition.x) / tick.Delta;
							velocity.Value.x = targetVelocityX;
						}

						controller.ControlOverVelocity.x = true;

						velocityUpdater.CompareAndUpdate(velocity);
						controllerUpdater.CompareAndUpdate(controller);
					})
					.WithReadOnly(seekingStateFromEntity)
					.WithReadOnly(relativeTeamFromEntity)
					.Schedule(inputDeps);

			m_ProjectileProvider.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}