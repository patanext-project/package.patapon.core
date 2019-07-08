using System;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class UnitPhysicsSystem : JobComponentSystem
	{
		// Right now, there is no collisions or things like that
		[RequireComponentTag(typeof(UnitDescription), typeof(EntityAuthority))]
		private struct Job : IJobForEach<UnitControllerState, GroundState, Translation, Velocity, UnitBaseSettings, UnitTargetPosition>
		{
			public float  DeltaTime;
			public float3 Gravity;

			private float MoveTowards(float current, float target, float maxDelta)
			{
				if (math.abs(target - current) <= maxDelta)
					return target;
				return current + Mathf.Sign(target - current) * maxDelta;
			}

			public void Execute(ref UnitControllerState controllerState, ref GroundState groundState, ref Translation translation, ref Velocity velocity, [ReadOnly] ref UnitBaseSettings unitSettings, [ReadOnly] ref UnitTargetPosition targetPosition)
			{
				var target = controllerState.OverrideTargetPosition ? controllerState.TargetPosition : targetPosition.Value;
				if (!controllerState.ControlOverVelocity.x)
				{
					if (groundState.Value)
					{
						var speed = math.lerp(math.abs(velocity.Value.x), unitSettings.MovementAttackSpeed, math.rcp(unitSettings.Weight) * 30 * DeltaTime);
						
						// Instead of just assigning the translation value here, we calculate the velocity between the new position and the previous position.
						var newPosX = MoveTowards(translation.Value.x, target.x, speed * DeltaTime);

						velocity.Value.x = (newPosX - translation.Value.x) / DeltaTime;
					}
					else
					{
						var acceleration = math.clamp(math.rcp(unitSettings.Weight), 0, 1) * 10;
						acceleration = math.min(acceleration * DeltaTime, 1) * 0.75f;
						
						velocity.Value.x = math.lerp(velocity.Value.x, 0, acceleration);
					}
				}

				if (!controllerState.ControlOverVelocity.y)
				{
					if (!groundState.Value)
						velocity.Value += Gravity * DeltaTime;
				}

				translation.Value += velocity.Value * DeltaTime;
				if (translation.Value.y < 0) // meh
					translation.Value.y = 0;

				groundState.Value = translation.Value.y <= 0;
				if (!controllerState.ControlOverVelocity.y && groundState.Value)
					velocity.Value.y = math.max(velocity.Value.y, 0);


				controllerState.ControlOverVelocity    = false;
				controllerState.OverrideTargetPosition = false;
				controllerState.PassThroughEnemies     = false;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				DeltaTime = GetSingleton<GameTimeComponent>().DeltaTime,
				Gravity   = new float3(0, -20f, 0)
			}.Schedule(this, inputDeps);
		}
	}
}