using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(ActionSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class DefaultBackwardRetreatSystem : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var dt                            = Time.DeltaTime;
			var unitDirectionFromEntity       = GetComponentDataFromEntity<UnitDirection>(true);
			var unitPlayStateFromEntity       = GetComponentDataFromEntity<UnitPlayState>(true);
			var translationFromEntity         = GetComponentDataFromEntity<Translation>();
			var unitControllerStateFromEntity = GetComponentDataFromEntity<UnitControllerState>();
			var velocityFromEntity            = GetComponentDataFromEntity<Velocity>();

			inputDeps =
				Entities
					.WithReadOnly(unitPlayStateFromEntity)
					.WithReadOnly(unitDirectionFromEntity)
					.WithNativeDisableParallelForRestriction(translationFromEntity)
					.WithNativeDisableParallelForRestriction(unitControllerStateFromEntity)
					.WithNativeDisableParallelForRestriction(velocityFromEntity)
					.ForEach((Entity entity, ref RhythmAbilityState state, ref DefaultRetreatAbility ability, in Owner owner) =>
					{
						if (state.ActiveId != ability.LastActiveId)
						{
							ability.IsRetreating = false;
							ability.ActiveTime   = 0;
							ability.LastActiveId = state.ActiveId;
						}

						var velocityUpdater        = velocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);
						var controllerStateUpdater = unitControllerStateFromEntity.GetUpdater(owner.Target).Out(out var controllerState);
						if (!state.IsActive && !state.IsStillChaining)
						{
							if (math.distance(ability.StartPosition, translationFromEntity[owner.Target].Value) > 2.5f && ability.ActiveTime > 0.1f)
							{
								velocity.Value.x = (ability.StartPosition.x - translationFromEntity[owner.Target].Value.x) * 3f;
								velocityUpdater.CompareAndUpdate(velocity);
							}

							ability.ActiveTime   = 0;
							ability.IsRetreating = false;
							return;
						}

						const float walkbackTime = 3.25f;

						var wasRetreating = ability.IsRetreating;
						ability.IsRetreating = ability.ActiveTime <= walkbackTime;

						var translation   = translationFromEntity[owner.Target];
						var unitSettings  = unitPlayStateFromEntity[owner.Target];
						var unitDirection = unitDirectionFromEntity[owner.Target];

						var retreatSpeed = unitSettings.MovementAttackSpeed * 3f;

						if (!wasRetreating && ability.IsRetreating)
						{
							ability.StartPosition = translation.Value;
							velocity.Value.x      = -unitDirection.Value * retreatSpeed;
						}

						// there is a little stop when the character is stopping retreating
						if (ability.ActiveTime >= DefaultRetreatAbility.StopTime && ability.ActiveTime <= walkbackTime)
							// if he weight more, he will stop faster
							velocity.Value.x = math.lerp(velocity.Value.x, 0, unitSettings.Weight * 0.25f * dt);

						if (!ability.IsRetreating && ability.ActiveTime > walkbackTime)
						{
							if (wasRetreating)
								// we add '2.8f' to boost the speed when backing up, so the unit can't chain retreat to go further
								ability.BackVelocity = math.abs(ability.StartPosition.x - translation.Value.x) * 2.8f;

							var newPosX = Mathf.MoveTowards(translation.Value.x, ability.StartPosition.x, ability.BackVelocity * dt);
							velocity.Value.x = (newPosX - translation.Value.x) / dt;
						}

						ability.ActiveTime += dt;

						controllerState.ControlOverVelocity.x = true;
						controllerStateUpdater.CompareAndUpdate(controllerState);
						velocityUpdater.CompareAndUpdate(velocity);
					})
					.Schedule(inputDeps);

			return inputDeps;
		}
	}
}