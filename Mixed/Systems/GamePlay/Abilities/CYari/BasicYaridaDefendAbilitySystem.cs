using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.Abilities.CYari;
using Patapon.Mixed.GamePlay.Physics;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Systems.GamePlay.CYari
{
	public unsafe class BasicYaridaDefendAbilitySystem : BaseAbilitySystem
	{
		private SpearProjectile.Provider m_ProjectileProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ProjectileProvider = World.GetOrCreateSystem<SpearProjectile.Provider>();
		}

		protected override void OnUpdate()
		{
			var tick                   = ServerTick;
			var impl                   = new BasicUnitAbilityImplementation(this);
			var seekingStateFromEntity = GetComponentDataFromEntity<UnitEnemySeekingState>(true);
			var chargeCommandFromEntity = GetComponentDataFromEntity<ChargeCommand>(true);

			var queueWriter = m_ProjectileProvider.GetEntityDelayedStream()
			                                      .AsParallelWriter();

			Entities
				.ForEach((Entity entity, int nativeThreadIndex, ref BasicYaridaDefendAbility ability, in AbilityState state, in AbilityEngineSet engineSet, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					var seekingState = seekingStateFromEntity[owner.Target];
					var statistics   = impl.UnitSettings[owner.Target];
					var unitPosition = impl.Translation[owner.Target].Value;
					var direction    = impl.UnitDirection[owner.Target].Value;

					var playStateUpdater  = impl.UnitPlayState.GetUpdater(owner.Target).Out(out var playState);
					var velocityUpdater   = impl.Velocity.GetUpdater(owner.Target).Out(out var velocity);
					var controllerUpdater = impl.Controller.GetUpdater(owner.Target).Out(out var controller);

					var attackStartTick = UTick.CopyDelta(tick, ability.AttackStartTick);

					if ((state.Phase & EAbilityPhase.ActiveOrChaining) != 0)
					{
						if ((state.Phase & EAbilityPhase.Chaining) != 0)
						{
							controller.ControlOverVelocity.x = true;
							velocity.Value.x                 = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 50 * tick.Delta);
						}
					}

					ability.NextAttackDelay -= tick.Delta;

					var throwOffset = new float3 {x = direction, y = 1.75f};
					var gravity     = new float3 {y = -10};
					if (ability.AttackStartTick > 0)
					{
						if (tick >= UTick.AddMs(attackStartTick, BasicYaridaDefendAbility.DelayThrowMs) && !ability.HasThrown)
						{
							queueWriter.Enqueue(new SpearProjectile.Create
							{
								Owner       = owner.Target,
								Position    = unitPosition + throwOffset,
								Velocity    = new float3(ability.ThrowVec.x * direction, ability.ThrowVec.y, 0),
								StartDamage = playState.Attack,
								Gravity     = gravity
							});

							ability.HasThrown = true;
						}
						else if (!ability.HasThrown)
						{
							controller.ControlOverVelocity.x = true;
						}

						// stop moving
						if (ability.HasThrown)
							velocity.Value.x = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 25 * tick.Delta);

						// stop attacking once the animation is done
						if (tick >= UTick.AddMs(attackStartTick, 500))
							ability.AttackStartTick = 0;
					}

					playStateUpdater.CompareAndUpdate(playState);
					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);

					impl.LocalToWorld.TryGet(seekingState.Enemy, out var enemyLtw);
					var targetPosition = enemyLtw.Position;

					var throwDeltaPosition = PredictTrajectory.Simple(throwOffset, new float3(ability.ThrowVec.x * direction, ability.ThrowVec.y, 0), new float3(0, -22, 0));
					targetPosition.x -= throwDeltaPosition.x;

					var outOfRange = seekingState.Distance > statistics.AttackSeekRange * 0.7f;
					if ((state.Phase & EAbilityPhase.Active) == 0 || seekingState.Enemy == default
					                                        || outOfRange && math.abs(targetPosition.x - unitPosition.x) > statistics.AttackSeekRange * 0.7f)
					{
						if ((state.Phase & EAbilityPhase.Active) != 0 && engineSet.Combo.IsFever)
						{
							playState.MovementReturnSpeed *= 1.8f;
							if (engineSet.Combo.IsPerfect)
								playState.MovementReturnSpeed *= 1.2f;

							controller.ControlOverVelocity.x = true;
						}

						return;
					}
					
					var distanceMercy = 1.8f;
					if (math.abs(targetPosition.x - unitPosition.x) < distanceMercy && ability.NextAttackDelay <= 0 && ability.AttackStartTick <= 0)
					{
						ability.NextAttackDelay = playState.AttackSpeed;
						ability.AttackStartTick = tick.AsUInt;
						ability.HasThrown       = false;

						var speed   = math.lerp(math.abs(velocity.Value.x), playState.MovementAttackSpeed, playState.GetAcceleration() * 5 * tick.Delta);
						var newPosX = Mathf.MoveTowards(unitPosition.x, targetPosition.x, speed * tick.Delta);

						if (!outOfRange)
						{
							var targetVelocityX = (newPosX - unitPosition.x) / tick.Delta;
							velocity.Value.x = targetVelocityX;
						}
					}
					else if (tick >= UTick.AddMs(attackStartTick, BasicYaridaDefendAbility.DelayThrowMs))
					{
						var speed   = math.lerp(math.abs(velocity.Value.x), playState.MovementAttackSpeed, playState.GetAcceleration() * 20 * tick.Delta);
						var newPosX = Mathf.MoveTowards(unitPosition.x, targetPosition.x, speed * tick.Delta);

						if (!outOfRange)
						{
							var targetVelocityX = (newPosX - unitPosition.x) / tick.Delta;
							velocity.Value.x = targetVelocityX;
						}
					}

					controller.ControlOverVelocity.x = !outOfRange;

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);
				})
				.WithReadOnly(seekingStateFromEntity)
				.WithReadOnly(chargeCommandFromEntity)
				.Run();
		}
	}
}