using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(RhythmAbilitySystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[AlwaysSynchronizeSystem]
	public class DefaultJumpAbilitySystem : AbsGameBaseSystem
	{
		protected override void OnUpdate()
		{
			var tick                          = ServerTick;
			var unitControllerStateFromEntity = GetComponentDataFromEntity<UnitControllerState>();
			var velocityFromEntity            = GetComponentDataFromEntity<SVelocity>();

			var impl = new BasicUnitAbilityImplementation(this);

			Entities
				.WithNativeDisableParallelForRestriction(unitControllerStateFromEntity)
				.WithNativeDisableParallelForRestriction(velocityFromEntity)
				.ForEach((Entity entity, ref DefaultJumpAbility ability, in AbilityState controller, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					var controllerStateUpdater = unitControllerStateFromEntity.GetUpdater(owner.Target).Out(out var controllerState);
					var velocityUpdater        = velocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);

					if (controller.ActivationVersion != ability.LastActiveId)
					{
						ability.IsJumping    = false;
						ability.ActiveTime   = 0;
						ability.LastActiveId = controller.ActivationVersion;
					}
					
					if ((controller.Phase & EAbilityPhase.ActiveOrChaining) == 0)
					{
						if (ability.IsJumping)
						{
							velocity.Value.y = math.max(0, velocity.Value.y - 60 * (ability.ActiveTime * 2));
							velocityUpdater.CompareAndUpdate(velocity);
						}

						ability.ActiveTime = 0;
						ability.IsJumping  = false;
						return;
					}

					var wasJumping = ability.IsJumping;
					ability.IsJumping = ability.ActiveTime <= 0.5f;

					if (!wasJumping && ability.IsJumping)
						velocity.Value.y                                                 = math.max(velocity.Value.y + 25, 30);
					else if (ability.IsJumping && velocity.Value.y > 0) velocity.Value.y = math.max(velocity.Value.y - 60 * tick.Delta, 0);

					if (ability.ActiveTime < 3.25f)
						velocity.Value.x = math.lerp(velocity.Value.x, 0, tick.Delta * (ability.ActiveTime + 1));

					if (!ability.IsJumping && velocity.Value.y > 0)
					{
						velocity.Value.y = math.max(velocity.Value.y - 10 * tick.Delta, 0);
						velocity.Value.y = math.lerp(velocity.Value.y, 0, 5 * tick.Delta);
					}

					ability.ActiveTime += tick.Delta;

					controllerState.ControlOverVelocity.x = ability.ActiveTime < 3.25f;
					controllerState.ControlOverVelocity.y = ability.ActiveTime < 2.5f;

					controllerStateUpdater.CompareAndUpdate(controllerState);
					velocityUpdater.CompareAndUpdate(velocity);
				})
				.Run();
		}
	}
}