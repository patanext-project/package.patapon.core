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
	public class YaridaFearSpearAbilitySystem : BaseAbilitySystem
	{
		private FearSpearProjectile.Provider m_ProjectileProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ProjectileProvider = World.GetOrCreateSystem<FearSpearProjectile.Provider>();
		}

		protected override void OnUpdate()
		{
			var tick                   = ServerTick;
			var impl                   = new BasicUnitAbilityImplementation(this);
			var seekingStateFromEntity = GetComponentDataFromEntity<UnitEnemySeekingState>(true);
			var marchCommandFromEntity = GetComponentDataFromEntity<MarchCommand>(true);

			var queueWriter = m_ProjectileProvider.GetEntityDelayedStream()
			                                      .AsParallelWriter();

			var rand = new Random((uint) Environment.TickCount);
			Entities
				.ForEach((Entity entity, int nativeThreadIndex, ref YaridaFearSpearAbility ability, ref DefaultSubsetMarch subSetMarch, in AbilityState state, in AbilityEngineSet engineSet, in Owner owner) =>
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

					subSetMarch.IsActive = (state.Phase & EAbilityPhase.Active) != 0 && marchCommandFromEntity.Exists(engineSet.Command);

					rand.state += (uint) entity.Index;

					ability.NextAttackDelay -= tick.Delta;

					var throwOffset = new float3 {x = direction * 2.5f, y = 1.4f};
					var gravity     = new float3 {y = -28f};
					if (ability.AttackStartTick > 0)
					{
						velocity.Value.y -= 14f * tick.Delta;

						if (tick >= UTick.AddMs(attackStartTick, YaridaFearSpearAbility.DelayThrowMs) && !ability.HasThrown)
						{
							var accuracy = AbilityUtility.CompileStat(engineSet.Combo, 0.03f, 1, 2, 3);
							queueWriter.Enqueue(new FearSpearProjectile.Create
							{
								Owner       = owner.Target,
								Position    = unitPosition + new float3(throwOffset.x, throwOffset.y - 0.25f, 0),
								Velocity    = {x = ability.ThrowVec.x * direction + accuracy * rand.NextFloat(), y = ability.ThrowVec.y},
								StartDamage = playState.Attack,
								Gravity     = gravity * 1.3f
							});

							ability.HasThrown = true;
						}
						else if (!ability.HasThrown)
						{
							velocity.Value.y                 -= 27.5f * tick.Delta;
							controller.ControlOverVelocity.x =  true;
						}

						// stop moving
						if (ability.HasThrown)
							velocity.Value.x = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 25f * tick.Delta);

						// stop attacking once the animation is done
						if (tick >= UTick.AddMs(attackStartTick, 500))
							ability.AttackStartTick = 0;
					}

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);

					if ((state.Phase & EAbilityPhase.ActiveOrChaining) == 0 || seekingState.Enemy == default)
						return;

					var targetPosition = impl.LocalToWorld[seekingState.Enemy].Position;
					var displacement   = PredictTrajectory.GetDisplacement(new float3(0, 14, 0), gravity, YaridaFearSpearAbility.DelayThrowMs * 0.001f);
					var throwDeltaPosition = PredictTrajectory.Simple(throwOffset + displacement, new float3
					{
						x = ability.ThrowVec.x * direction,
						y = ability.ThrowVec.y
					}, gravity, yLimit: 0.25f);
					targetPosition.x -= throwDeltaPosition.x;

					var distanceMercy = 2.25f;
					if (targetPosition.x - unitPosition.x < distanceMercy && ability.NextAttackDelay <= 0 && ability.AttackStartTick <= 0 && unitPosition.y < 0.25f)
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
						velocity.Value.y = math.max(velocity.Value.y + 24, 24);
					}
					else if (tick >= UTick.AddMs(attackStartTick, YaridaFearSpearAbility.DelayThrowMs))
					{
						if (impl.IsGrounded(owner.Target))
							velocity.Value.x = AbilityUtility.GetTargetVelocityX(new AbilityUtility.GetTargetVelocityParameters
							{
								TargetPosition   = targetPosition,
								PreviousPosition = unitPosition,
								PreviousVelocity = velocity.Value,
								PlayState        = playState,
								Acceleration     = 20,
								Tick             = tick
							});
						else
							velocity.Value.x = math.lerp(velocity.Value.x, 0, math.min(1, playState.Weight * 10f * tick.Delta));
					}

					controller.ControlOverVelocity.x = true;

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);
				})
				.WithReadOnly(seekingStateFromEntity)
				.WithReadOnly(marchCommandFromEntity)
				.Schedule();

			m_ProjectileProvider.AddJobHandleForProducer(Dependency);
		}
	}
}