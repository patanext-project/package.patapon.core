using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(ActionSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class DefaultJumpAbilitySystem : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var tick                          = ServerTick;
			var unitControllerStateFromEntity = GetComponentDataFromEntity<UnitControllerState>();
			var velocityFromEntity            = GetComponentDataFromEntity<Velocity>();

			inputDeps =
				Entities
					.WithNativeDisableParallelForRestriction(unitControllerStateFromEntity)
					.WithNativeDisableParallelForRestriction(velocityFromEntity)
					.ForEach((Entity entity, ref RhythmAbilityState state, ref DefaultJumpAbility ability, in Owner owner) =>
					{
						var controllerStateUpdater = unitControllerStateFromEntity.GetUpdater(owner.Target).Out(out var controllerState);
						var velocityUpdater        = velocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);

						if (state.ActiveId != ability.LastActiveId)
						{
							ability.IsJumping    = false;
							ability.ActiveTime   = 0;
							ability.LastActiveId = state.ActiveId;
						}

						if (!state.IsActive && !state.IsStillChaining)
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
							velocity.Value.y                                                 = math.max(velocity.Value.y + 30, 30);
						else if (ability.IsJumping && velocity.Value.y > 0) velocity.Value.y = math.max(velocity.Value.y - 60 * tick.Delta, 0);

						if (ability.ActiveTime < 3.25f)
							velocity.Value.x = math.lerp(velocity.Value.x, 0, tick.Delta * (ability.ActiveTime + 1));

						if (!ability.IsJumping && wasJumping)
							velocity.Value.y = 0;

						ability.ActiveTime += tick.Delta;

						controllerState.ControlOverVelocity.x = ability.ActiveTime < 3.25f;
						controllerState.ControlOverVelocity.y = ability.ActiveTime < 2.5f;

						controllerStateUpdater.CompareAndUpdate(controllerState);
						velocityUpdater.CompareAndUpdate(velocity);
					})
					.Schedule(inputDeps);

			return inputDeps;
		}
	}
}