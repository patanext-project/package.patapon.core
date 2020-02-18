using System;
using Systems.GamePlay.CYari;
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
	public class DefaultSubsetMarchAbilitySystem : BaseAbilitySystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var dt                            = Time.DeltaTime;
			var unitTargetControlFromEntity   = GetComponentDataFromEntity<UnitTargetControlTag>(true);
			var unitDirectionFromEntity       = GetComponentDataFromEntity<UnitDirection>(true);
			var targetOffsetFromEntity        = GetComponentDataFromEntity<UnitTargetOffset>(true);
			var unitControllerStateFromEntity = GetComponentDataFromEntity<UnitControllerState>();
			var relativeTargetFromEntity      = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);

			var impl = new BasicUnitAbilityImplementation(this);

			Entities
				.WithReadOnly(unitTargetControlFromEntity)
				.WithReadOnly(unitDirectionFromEntity)
				.WithReadOnly(targetOffsetFromEntity)
				.WithReadOnly(relativeTargetFromEntity)
				.WithNativeDisableParallelForRestriction(unitControllerStateFromEntity)
				.ForEach((Entity entity, ref DefaultSubsetMarch subSet, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					if (!subSet.IsActive)
					{
						subSet.Delta = 0.0f;
						return;
					}

					var targetOffset  = targetOffsetFromEntity[owner.Target];
					var groundState   = impl.GroundStateFromEntity[owner.Target];
					var unitPlayState = impl.UnitPlayStateFromEntity[owner.Target];

					Relative<UnitTargetDescription> relativeTarget;
					if (!relativeTargetFromEntity.TryGet(entity, out relativeTarget))
						if (!relativeTargetFromEntity.TryGet(owner.Target, out relativeTarget))
							return;

					subSet.Delta += dt;

					var   targetPosition = impl.TranslationFromEntity[relativeTarget.Target].Value;
					float acceleration, walkSpeed;
					int   direction;

					// Cursor movement
					if ((subSet.SubSet & DefaultSubsetMarch.ESubSet.Cursor) != 0
					    && unitTargetControlFromEntity.Exists(owner.Target)
					    && subSet.Delta <= 3.75f)
					{
						direction = unitDirectionFromEntity[owner.Target].Value;

						// a different acceleration (not using the unit weight)
						acceleration = subSet.AccelerationFactor;
						acceleration = math.min(acceleration * dt, 1);

						walkSpeed      =  unitPlayState.MovementSpeed;
						targetPosition += walkSpeed * direction * (subSet.Delta > 0.25f ? 1 : math.lerp(2, 1, subSet.Delta + 0.25f)) * acceleration;

						impl.TranslationFromEntity[relativeTarget.Target] = new Translation {Value = targetPosition};
					}

					// Character movement
					if ((subSet.SubSet & DefaultSubsetMarch.ESubSet.Movement) != 0)
					{
						if (!groundState.Value)
							return;
						
						var velocity = impl.VelocityFromEntity[owner.Target];

						// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
						acceleration = math.clamp(math.rcp(unitPlayState.Weight), 0, 1) * subSet.AccelerationFactor * 50;
						acceleration = math.min(acceleration * dt, 1);

						walkSpeed = unitPlayState.MovementSpeed;
						direction = Math.Sign(targetPosition.x + targetOffset.Value - impl.TranslationFromEntity[owner.Target].Value.x);

						velocity.Value.x                      = math.lerp(velocity.Value.x, walkSpeed * direction, acceleration);
						impl.VelocityFromEntity[owner.Target] = velocity;

						var controllerState = unitControllerStateFromEntity[owner.Target];
						controllerState.ControlOverVelocity.x       = true;
						unitControllerStateFromEntity[owner.Target] = controllerState;
					}
				})
				.Run();

			return default;
		}
	}
}