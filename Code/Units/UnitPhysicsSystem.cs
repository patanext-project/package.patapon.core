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
		private struct Job : IJobForEach<UnitControllerState, Translation, Velocity, UnitBaseSettings, UnitTargetPosition>
		{
			public float  DeltaTime;
			public float3 Gravity;

			private float MoveTowards(float current, float target, float maxDelta)
			{
				if (math.abs(target - current) <= maxDelta)
					return target;
				return current + Mathf.Sign(target - current) * maxDelta;
			}

			public void Execute(ref UnitControllerState controllerState, ref Translation translation, ref Velocity velocity, [ReadOnly] ref UnitBaseSettings unitSettings, [ReadOnly] ref UnitTargetPosition targetPosition)
			{
				var target = controllerState.OverrideTargetPosition ? controllerState.TargetPosition : targetPosition.Value;
				if (!controllerState.ControlOverVelocity)
				{
					// We should instead have another system to damp the velocity... (along with settings + taking in account weight)
					var acceleration = math.clamp(math.rcp(unitSettings.Weight), 0, 1) * 10;
					acceleration = math.min(acceleration * DeltaTime, 1) * 1.5f;

					// Instead of just assigning the translation value here, we calculate the velocity between the new position and the previous position.
					var newPosX = MoveTowards(translation.Value.x, target.x, acceleration);
					velocity.Value.x = (newPosX - translation.Value.x) / DeltaTime;
				}

				translation.Value += velocity.Value * DeltaTime;


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
				Gravity   = new float3(0, -10, 0)
			}.Schedule(this, inputDeps);
		}
	}
}