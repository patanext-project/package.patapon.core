using Systems.GamePlay.CYari;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
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
	public unsafe class TaterazayBasicDefendFrontalAbilitySystem : BaseAbilitySystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var tick                     = ServerTick;
			var impl                     = new BasicUnitAbilityImplementation(this);
			var relativeTargetFromEntity = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);
			var isPredicted              = World.GetExistingSystem<RhythmAbilitySystemGroup>().IsPredicted;

			Entities
				.ForEach((Entity entity, int nativeThreadIndex, ref BasicTaterazayDefendFrontalAbility ability, in AbilityState state, in AbilityEngineSet engineSet, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					var playStateUpdater  = impl.UnitPlayStateFromEntity.GetUpdater(owner.Target).Out(out var playState);
					var velocityUpdater   = impl.VelocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);
					var controllerUpdater = impl.ControllerFromEntity.GetUpdater(owner.Target).Out(out var controller);

					if ((state.Phase & EAbilityPhase.Chaining) != 0 && isPredicted)
					{
						controller.ControlOverVelocity.x = true;
						velocity.Value.x                 = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 50 * tick.Delta);
					}

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);
					playStateUpdater.CompareAndUpdate(playState);

					if ((state.Phase & EAbilityPhase.Active) == 0)
						return;

					var tryGetChain = stackalloc[] {entity, owner.Target};
					if (!relativeTargetFromEntity.TryGetChain(tryGetChain, 2, out var relativeTarget)) return;

					var unitPosition   = impl.TranslationFromEntity[owner.Target].Value;
					var direction      = impl.UnitDirectionFromEntity[owner.Target].Value;
					var targetPosition = impl.TranslationFromEntity[relativeTarget.Target].Value.x + ability.Range * direction;

					if (isPredicted)
					{
						velocity.Value.x                 = AbilityUtility.GetTargetVelocityX(targetPosition, unitPosition, velocity.Value, playState, 25f, tick,
							deaccel_distance: 0.0f, deaccel_distance_max: 0.5f);
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