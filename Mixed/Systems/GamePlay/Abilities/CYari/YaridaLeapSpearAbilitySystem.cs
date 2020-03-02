using System;
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
using Unity.NetCode;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Systems.GamePlay.CYari
{
	public class YaridaLeapSpearAbilitySystem : BaseAbilitySystem
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
			var queueWriter = m_ProjectileProvider.GetEntityDelayedStream()
			                                      .AsParallelWriter();

			var rand = new Random((uint) Environment.TickCount);
			Entities
				.ForEach((Entity entity, int nativeThreadIndex, ref YaridaLeapSpearAbility ability, in AbilityState state, in AbilityEngineSet engineSet, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					var seekingState = seekingStateFromEntity[owner.Target];
					var playState    = impl.UnitPlayState[owner.Target];
					var unitPosition = impl.Translation[owner.Target].Value;
					var direction    = impl.UnitDirection[owner.Target].Value;

					var velocityUpdater   = impl.Velocity.GetUpdater(owner.Target).Out(out var velocity);
					var controllerUpdater = impl.Controller.GetUpdater(owner.Target).Out(out var controller);

					var attackStartTick = UTick.CopyDelta(tick, ability.AttackStartTick);

					if ((state.Phase & EAbilityPhase.Chaining) != 0)
					{
						controller.ControlOverVelocity.x = true;

						var accelF = impl.IsGrounded(owner.Target) ? 50 : 1f;
						velocity.Value.x = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * accelF * tick.Delta);
					}

					rand.state += (uint) entity.Index;

					ability.NextAttackDelay -= tick.Delta;

					var throwOffset = new float3 {x = direction, y = 1.4f};
					var gravity     = new float3 {y = -26f};
					if (ability.AttackStartTick > 0)
					{
						if (tick >= UTick.AddMs(attackStartTick, YaridaLeapSpearAbility.DelayThrowMs) && !ability.HasThrown)
						{
							var accuracy = AbilityUtility.CompileStat(engineSet.Combo, 0.01f, 1, 2, 3);
							queueWriter.Enqueue(new SpearProjectile.Create
							{
								Owner       = owner.Target,
								Position    = unitPosition + new float3(throwOffset.x, throwOffset.y, 0),
								Velocity    = {x = ability.ThrowVec.x * direction + accuracy * rand.NextFloat(), y = ability.ThrowVec.y},
								StartDamage = playState.Attack,
								Gravity     = gravity * 0.9f
							});

							ability.HasThrown = true;
							velocity.Value.y  = math.lerp(velocity.Value.y, 0, 0.5f);
						}
						else if (!ability.HasThrown)
							controller.ControlOverVelocity.x = true;

						// stop moving
						if (ability.HasThrown)
							velocity.Value.x = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 10f * tick.Delta);

						// stop attacking once the animation is done
						if (tick >= UTick.AddMs(attackStartTick, 500))
							ability.AttackStartTick = 0;
					}

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);

					if ((state.Phase & EAbilityPhase.Active) == 0 || seekingState.Enemy == default)
						return;

					var targetPosition = impl.LocalToWorld[seekingState.Enemy].Position;
					var displacement   = PredictTrajectory.GetDisplacement(new float3(0, 16, 0), gravity, YaridaLeapSpearAbility.DelayThrowMs * 0.001f);
					var throwDeltaPosition = PredictTrajectory.Simple(throwOffset + displacement, new float3
					{
						x = ability.ThrowVec.x * direction,
						y = ability.ThrowVec.y
					}, gravity, yLimit: 0.25f);
					targetPosition.x -= throwDeltaPosition.x;

					var distanceMercy = 2.25f;
					if (math.abs(targetPosition.x - unitPosition.x) < distanceMercy && ability.NextAttackDelay <= 0 && ability.AttackStartTick <= 0 && unitPosition.y < 0.25f)
					{
						// launching two spear while jumping should be hard, so we give a bigger attack delay... (x1.75) at the start....
						ability.NextAttackDelay = playState.AttackSpeed;
						ability.AttackStartTick = tick.AsUInt;
						ability.HasThrown       = false;

						velocity.Value.x = AbilityUtility.GetTargetVelocityX(new AbilityUtility.GetTargetVelocityParameters
						{
							TargetPosition   = targetPosition,
							PreviousPosition = unitPosition,
							PreviousVelocity = velocity.Value,
							PlayState        = playState,
							Acceleration     = 25,
							Tick             = tick
						});
						velocity.Value.y = math.max(velocity.Value.y + 18, 18);
					}
					else if (tick >= UTick.AddMs(attackStartTick, YaridaLeapSpearAbility.DelayThrowMs))
					{
						if (impl.IsGrounded(owner.Target))
							velocity.Value.x = AbilityUtility.GetTargetVelocityX(new AbilityUtility.GetTargetVelocityParameters
							{
								TargetPosition   = targetPosition,
								PreviousPosition = unitPosition,
								PreviousVelocity = velocity.Value,
								PlayState        = playState,
								Acceleration     = 10,
								Tick             = tick
							});
						else
							velocity.Value.x = math.lerp(velocity.Value.x, 0, playState.Weight * 10f * tick.Delta);
					}

					controller.ControlOverVelocity.x = true;

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);
				})
				.WithReadOnly(seekingStateFromEntity)
				.Schedule();

			m_ProjectileProvider.AddJobHandleForProducer(Dependency);
		}
	}
}