using System;
using P4.Core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
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
		[BurstCompile]
		[RequireComponentTag(typeof(UnitDescription), typeof(EntityAuthority))]
		private struct Job : IJobForEachWithEntity<UnitControllerState, GroundState, Translation, Velocity, UnitPlayState, UnitTargetPosition>
		{
			public float  DeltaTime;
			public float3 Gravity;

			[ReadOnly] public ComponentDataFromEntity<UnitDirection> UnitDirectionFromEntity;
			[ReadOnly] public ComponentDataFromEntity<Relative<TeamDescription>> RelativeTeamFromEntity;
			[ReadOnly] public BufferFromEntity<TeamEnemies>                      TeamEnemiesFromEntity;
			[ReadOnly] public ComponentDataFromEntity<TeamBlockMovableArea>      BlockMovableAreaFromEntity;

			private float MoveTowards(float current, float target, float maxDelta)
			{
				if (math.abs(target - current) <= maxDelta)
					return target;
				return current + Mathf.Sign(target - current) * maxDelta;
			}

			public void Execute(Entity                             entity,          int                               jobIndex,
			                    ref            UnitControllerState controllerState, ref            GroundState        groundState,
			                    ref            Translation         translation,     ref            Velocity           velocity,
			                    [ReadOnly] ref UnitPlayState       unitPlayState,   [ReadOnly] ref UnitTargetPosition targetPosition)
			{
				if (velocity.Value.y > 0)
					groundState.Value = false;

				var previousPosition = translation.Value;
				var target = controllerState.OverrideTargetPosition ? controllerState.TargetPosition : targetPosition.Value;
				if (!controllerState.ControlOverVelocity.x)
				{
					if (groundState.Value)
					{
						var speed = math.lerp(math.abs(velocity.Value.x), unitPlayState.MovementAttackSpeed, math.rcp(unitPlayState.Weight) * 30 * DeltaTime);

						// Instead of just assigning the translation value here, we calculate the velocity between the new position and the previous position.
						var newPosX = MoveTowards(translation.Value.x, target.x, speed * DeltaTime);

						velocity.Value.x = (newPosX - translation.Value.x) / DeltaTime;
					}
					else
					{
						var acceleration = math.clamp(math.rcp(unitPlayState.Weight), 0, 1) * 10;
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

				if (!controllerState.PassThroughEnemies && RelativeTeamFromEntity.Exists(entity))
				{
					var relativeTeam = RelativeTeamFromEntity[entity];
					if (TeamEnemiesFromEntity.Exists(relativeTeam.Target))
					{
						var enemies = TeamEnemiesFromEntity[relativeTeam.Target];
						for (var i = 0; i != enemies.Length; i++)
						{
							if (!BlockMovableAreaFromEntity.Exists(enemies[i].Target))
								continue;

							var area = BlockMovableAreaFromEntity[enemies[i].Target];
							// If the new position is superior the area and the previous one inferior, teleport back to the area.
							if (translation.Value.x > area.LeftX && previousPosition.x <= area.LeftX)
							{
								Debug.Log("superior");
								translation.Value.x = area.LeftX;
							}

							if (translation.Value.x < area.RightX && previousPosition.x >= area.RightX)
							{
								Debug.Log("inferior");
								translation.Value.x = area.RightX;
							}
							
							// if it's inside...
							if (translation.Value.x > area.LeftX && translation.Value.x < area.RightX)
							{
								var unitDirection = UnitDirectionFromEntity[entity];
								if (unitDirection.IsLeft)
									translation.Value = area.LeftX;
								else if (unitDirection.IsRight)
									translation.Value = area.RightX;
							}
						}
					}
				}

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
				Gravity   = new float3(0, -20f, 0),

				RelativeTeamFromEntity     = GetComponentDataFromEntity<Relative<TeamDescription>>(true),
				BlockMovableAreaFromEntity = GetComponentDataFromEntity<TeamBlockMovableArea>(true),
				TeamEnemiesFromEntity      = GetBufferFromEntity<TeamEnemies>(true),
				UnitDirectionFromEntity = GetComponentDataFromEntity<UnitDirection>(true)
			}.Schedule(this, inputDeps);
		}
	}
}