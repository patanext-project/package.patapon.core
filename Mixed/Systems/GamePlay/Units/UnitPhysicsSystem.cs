using Patapon.Mixed.GamePlay.Team;
using Patapon.Mixed.Rules;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Mixed.GamePlay
{
	[UpdateInGroup(typeof(UnitPhysicSystemGroup))]
	[UpdateAfter(typeof(TeamBlockMovableAreaSystem))]
	public class UnitPhysicsSystem : JobGameBaseSystem
	{
		private static float MoveTowards(float current, float target, float maxDelta)
		{
			if (math.abs(target - current) <= maxDelta)
				return target;
			return current + Mathf.Sign(target - current) * maxDelta;
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var dt                       = Time.DeltaTime;
			var gravity                  = new float3(0, -20f, 0);
			var livableHealthFromEntity  = GetComponentDataFromEntity<LivableHealth>(true);
			var relativeTargetFromEntity = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);
			var translationFromEntity    = GetComponentDataFromEntity<Translation>(true);
			var isServer = IsServer;

			if (!isServer && GetSingleton<P4NetworkRules.Data>().AbilityUsePredicted == false)
				return inputDeps;

			inputDeps =
				Entities
					.WithAll<UnitDescription>()
					.ForEach((Entity                  entity,
					          ref UnitControllerState controllerState, ref GroundState groundState, ref Translation translation, ref Velocity velocity,
					          in  UnitPlayState       unitPlayState) =>
					{
						if (velocity.Value.y > 0)
							groundState.Value = false;

						var previousPosition = translation.Value;
						var target = controllerState.OverrideTargetPosition || !relativeTargetFromEntity.Exists(entity)
							? controllerState.TargetPosition
							: translationFromEntity[relativeTargetFromEntity[entity].Target].Value.x;

						if (livableHealthFromEntity.Exists(entity) && livableHealthFromEntity[entity].IsDead)
						{
							controllerState.ControlOverVelocity.x = true;
							if (groundState.Value)
								velocity.Value.x = math.lerp(velocity.Value.x, 0, 2.5f * dt);
						}

						if (!controllerState.ControlOverVelocity.x)
						{
							// todo: find a good way for client to predict that nicely
							if (groundState.Value)
							{
								var speed = math.lerp(math.abs(velocity.Value.x), unitPlayState.MovementReturnSpeed, math.rcp(unitPlayState.Weight) * 30 * dt);

								// Instead of just assigning the translation value here, we calculate the velocity between the new position and the previous position.
								var newPosX = MoveTowards(translation.Value.x, target, speed * dt);

								velocity.Value.x = (newPosX - translation.Value.x) / dt;
							}
							else
							{
								var acceleration = math.clamp(math.rcp(unitPlayState.Weight), 0, 1) * 10;
								acceleration = math.min(acceleration * dt, 1) * 0.75f;

								velocity.Value.x = math.lerp(velocity.Value.x, 0, acceleration);
							}
						}

						if (!controllerState.ControlOverVelocity.y)
							if (!groundState.Value)
								velocity.Value += gravity * dt;

						for (var v = 0; v != 3; v++)
							velocity.Value[v] = math.isnan(velocity.Value[v]) ? 0.0f : velocity.Value[v];


						translation.Value += velocity.Value * dt;
						if (translation.Value.y < 0) // meh
							translation.Value.y = 0;

						groundState.Value = translation.Value.y <= 0;
						if (!controllerState.ControlOverVelocity.y && groundState.Value)
							velocity.Value.y = math.max(velocity.Value.y, 0);

						for (var v = 0; v != 3; v++)
							translation.Value[v] = math.isnan(translation.Value[v]) ? 0.0f : translation.Value[v];

						controllerState.ControlOverVelocity    = false;
						controllerState.OverrideTargetPosition = false;
						controllerState.PassThroughEnemies     = false;
						controllerState.PreviousPosition       = previousPosition;

						Debug.DrawRay(translation.Value, Vector3.up, Color.magenta);
					})
					.WithReadOnly(livableHealthFromEntity)
					.WithReadOnly(relativeTargetFromEntity)
					.WithReadOnly(translationFromEntity)
					.WithNativeDisableContainerSafetyRestriction(translationFromEntity) // aliasing...
					.Schedule(inputDeps);

			return inputDeps;
		}
	}
}