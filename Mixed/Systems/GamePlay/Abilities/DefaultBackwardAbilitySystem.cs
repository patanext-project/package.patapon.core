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

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(ActionSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class DefaultBackwardAbilitySystem : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var dt                            = Time.DeltaTime;
			var unitTargetControlFromEntity   = GetComponentDataFromEntity<UnitTargetControlTag>(true);
			var unitDirectionFromEntity       = GetComponentDataFromEntity<UnitDirection>(true);
			var unitPlayStateFromEntity       = GetComponentDataFromEntity<UnitPlayState>(true);
			var translationFromEntity         = GetComponentDataFromEntity<Translation>();
			var groundStateFromEntity         = GetComponentDataFromEntity<GroundState>(true);
			var targetOffsetFromEntity        = GetComponentDataFromEntity<UnitTargetOffset>(true);
			var unitControllerStateFromEntity = GetComponentDataFromEntity<UnitControllerState>();
			var velocityFromEntity            = GetComponentDataFromEntity<Velocity>();
			var relativeTargetFromEntity      = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);

			inputDeps =
				Entities
					.WithReadOnly(unitTargetControlFromEntity)
					.WithReadOnly(unitPlayStateFromEntity)
					.WithReadOnly(groundStateFromEntity)
					.WithReadOnly(unitDirectionFromEntity)
					.WithReadOnly(targetOffsetFromEntity)
					.WithReadOnly(relativeTargetFromEntity)
					.WithNativeDisableParallelForRestriction(translationFromEntity)
					.WithNativeDisableParallelForRestriction(unitControllerStateFromEntity)
					.WithNativeDisableParallelForRestriction(velocityFromEntity)
					.ForEach((Entity entity, ref RhythmAbilityState state, ref DefaultBackwardAbility backwardAbility, in Owner owner) =>
					{
						if (!state.IsActive)
						{
							backwardAbility.Delta = 0.0f;
							return;
						}

						var targetOffset  = targetOffsetFromEntity[owner.Target];
						var groundState   = groundStateFromEntity[owner.Target];
						var unitPlayState = unitPlayStateFromEntity[owner.Target];
						if (state.Combo.IsFever && state.Combo.Score >= 50)
						{
							unitPlayState.MovementSpeed *= 1.2f;
						}

						if (!groundState.Value)
							return;

						Relative<UnitTargetDescription> relativeTarget;
						if (!relativeTargetFromEntity.TryGet(entity, out relativeTarget))
							if (!relativeTargetFromEntity.TryGet(owner.Target, out relativeTarget))
								return;

						backwardAbility.Delta += dt;

						var   targetTranslationUpdater = translationFromEntity.GetUpdater(relativeTarget.Target).Out(out var targetPosition);
						float acceleration, walkSpeed;
						int   direction;

						if (unitTargetControlFromEntity.Exists(owner.Target))
						{
							direction = unitDirectionFromEntity[owner.Target].Value;

							// a different acceleration (not using the unit weight)
							acceleration = backwardAbility.AccelerationFactor;
							acceleration = math.min(acceleration * dt, 1);

							backwardAbility.Delta += dt;

							walkSpeed            =  unitPlayState.MovementSpeed * -0.5f;
							targetPosition.Value += walkSpeed * direction * (backwardAbility.Delta > 0.5f ? 1 : math.lerp(4, 1, backwardAbility.Delta + 0.5f)) * acceleration;
							targetTranslationUpdater.Update(targetPosition);
						}

						var velocityUpdater        = velocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);
						var controllerStateUpdater = unitControllerStateFromEntity.GetUpdater(owner.Target).Out(out var controllerState);

						// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
						acceleration = math.clamp(math.rcp(unitPlayState.Weight), 0, 1) * backwardAbility.AccelerationFactor * 50;
						acceleration = math.min(acceleration * dt, 1);

						walkSpeed = unitPlayState.MovementSpeed * 0.5f;
						// if we're near, let's slow down
						var dist = math.distance(targetPosition.Value.x, translationFromEntity[owner.Target].Value.x);
						if (dist < 0.5f)
						{
							walkSpeed *= math.clamp(dist * 0.5f, 0.5f, 1.0f);
						}

						direction = System.Math.Sign(targetPosition.Value.x + targetOffset.Value - translationFromEntity[owner.Target].Value.x);

						velocity.Value.x                 = math.lerp(velocity.Value.x, walkSpeed * direction, acceleration);
						velocityFromEntity[owner.Target] = velocity;

						controllerState.ControlOverVelocity.x = true;

						velocityUpdater.CompareAndUpdate(velocity);
						controllerStateUpdater.CompareAndUpdate(controllerState);
					})
					.Schedule(inputDeps);

			return inputDeps;
		}
	}
}