using Systems.GamePlay.CYari;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities.CTate;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Systems.GamePlay.CTate
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.ClientAndServer)]
	public unsafe class BasicTaterazayDefendAbilitySystem : BaseAbilitySystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var tick                     = ServerTick;
			var impl                     = new BasicUnitAbilityImplementation(this);
			var relativeTargetFromEntity = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);
			var isPredicted              = World.GetExistingSystem<RhythmAbilitySystemGroup>().IsPredicted;

			Entities
				.ForEach((Entity entity, int nativeThreadIndex, ref RhythmAbilityState state, ref BasicTaterazayDefendAbility ability, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					var playStateUpdater  = impl.UnitPlayStateFromEntity.GetUpdater(owner.Target).Out(out var playState);
					var velocityUpdater   = impl.VelocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);
					var controllerUpdater = impl.ControllerFromEntity.GetUpdater(owner.Target).Out(out var controller);

					if (state.IsStillChaining)
					{
						if (!state.IsActive && isPredicted)
						{
							controller.ControlOverVelocity.x = true;
							velocity.Value.x                 = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 50 * tick.Delta);
						}

						var defense = playState.Defense * 0.5f;

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

					playStateUpdater.CompareAndUpdate(playState);
					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);

					if (!state.IsActive)
						return;

					var tryGetChain = stackalloc[] {entity, owner.Target};
					if (!relativeTargetFromEntity.TryGetChain(tryGetChain, 2, out var relativeTarget)) return;

					var unitPosition = impl.TranslationFromEntity[owner.Target].Value;

					if (state.Combo.IsFever)
					{
						playState.MovementAttackSpeed *= 1.4f;
						if (state.Combo.IsPerfect)
							playState.MovementAttackSpeed *= 1.2f;
					}

					if (isPredicted)
					{
						var speed   = math.lerp(math.abs(velocity.Value.x), playState.MovementAttackSpeed, playState.GetAcceleration() * 5 * tick.Delta);
						var newPosX = Mathf.MoveTowards(unitPosition.x, impl.TranslationFromEntity[relativeTarget.Target].Value.x, speed * tick.Delta);

						var targetVelocityX = (newPosX - unitPosition.x) / tick.Delta;
						velocity.Value.x = targetVelocityX;

						controller.ControlOverVelocity.x = true;
					}

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);
				})
				.WithReadOnly(relativeTargetFromEntity)
				.Run();

			return default;
		}
	}
}