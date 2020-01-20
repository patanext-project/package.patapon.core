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

namespace Systems.GamePlay.CTate
{
	[UpdateInGroup(typeof(RhythmAbilitySystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public unsafe class BasicTaterazayDefendFrontalAbilitySystem : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var tick                     = ServerTick;
			var impl                     = new BasicUnitAbilityImplementation(this);
			var relativeTargetFromEntity = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);
			var isPredicted              = World.GetExistingSystem<RhythmAbilitySystemGroup>().IsPredicted;

			inputDeps =
				Entities
					.ForEach((Entity entity, int nativeThreadIndex, ref RhythmAbilityState state, ref BasicTaterazayDefendFrontalAbility ability, in Owner owner) =>
					{
						if (!impl.CanExecuteAbility(owner.Target))
							return;
						
						Entity* tryGetChain = stackalloc[] {entity, owner.Target};
						if (!relativeTargetFromEntity.TryGetChain(tryGetChain, 2, out var relativeTarget))
						{
							Debug.Log("no");
							return;
						}

						var unitPosition = impl.TranslationFromEntity[owner.Target].Value;
						var direction    = impl.UnitDirectionFromEntity[owner.Target].Value;

						var playStateUpdater = impl.UnitPlayStateFromEntity.GetUpdater(owner.Target).Out(out var playState);
						var velocityUpdater   = impl.VelocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);
						var controllerUpdater = impl.ControllerFromEntity.GetUpdater(owner.Target).Out(out var controller);

						if (state.IsStillChaining)
						{
							if (!state.IsActive && isPredicted)
							{
								controller.ControlOverVelocity.x = true;
								velocity.Value.x                 = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 50 * tick.Delta);
							}

							float defense = playState.Defense * 0.5f;

							playState.ReceiveDamagePercentage *= 0.7f;
							if (state.Combo.IsFever)
							{
								playState.ReceiveDamagePercentage *= 0.8f;
								defense                           *= 1.2f;
								if (state.Combo.IsPerfect)
								{
									playState.ReceiveDamagePercentage *= 0.9f;
									defense                           *= 1.2f;
								}
							}

							playState.Defense += (int) defense;
						}

						velocityUpdater.CompareAndUpdate(velocity);
						controllerUpdater.CompareAndUpdate(controller);
						playStateUpdater.CompareAndUpdate(playState);

						if (!state.IsActive)
							return;

						if (state.Combo.IsFever)
						{
							playState.MovementAttackSpeed *= 1.8f;
							if (state.Combo.IsPerfect)
								playState.MovementAttackSpeed *= 1.2f;
						}

						if (isPredicted)
						{
							var targetPosition = impl.TranslationFromEntity[relativeTarget.Target].Value.x + (ability.Range * direction);
							var speed          = math.lerp(math.abs(velocity.Value.x), playState.MovementAttackSpeed, playState.GetAcceleration() * 50 * tick.Delta);
							var newPosX        = Mathf.MoveTowards(unitPosition.x, targetPosition, speed * tick.Delta);

							var targetVelocityX = (newPosX - unitPosition.x) / tick.Delta;
							velocity.Value.x = targetVelocityX;

							controller.ControlOverVelocity.x = true;
						}

						velocityUpdater.CompareAndUpdate(velocity);
						controllerUpdater.CompareAndUpdate(controller);
					})
					.WithReadOnly(relativeTargetFromEntity)
					.Schedule(inputDeps);

			return inputDeps;
		}
	}
}