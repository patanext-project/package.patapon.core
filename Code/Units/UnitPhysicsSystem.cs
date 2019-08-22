using Patapon4TLB.Default;
using Patapon4TLBCore;
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
		[RequireComponentTag(typeof(UnitDescription))]
		private struct Job : IJobForEachWithEntity<UnitControllerState, GroundState, Translation, Velocity, UnitPlayState>
		{
			public float  DeltaTime;
			public float3 Gravity;

			[ReadOnly] public ComponentDataFromEntity<LivableHealth> LivableHealthFromEntity;
			[ReadOnly] public ComponentDataFromEntity<Relative<UnitTargetDescription>> RelativeTargetFromEntity;
			[ReadOnly] public ComponentDataFromEntity<Translation>                     TranslationFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitDirection>                   UnitDirectionFromEntity;
			[ReadOnly] public ComponentDataFromEntity<Relative<TeamDescription>>       RelativeTeamFromEntity;
			[ReadOnly] public BufferFromEntity<TeamEnemies>                            TeamEnemiesFromEntity;
			[ReadOnly] public ComponentDataFromEntity<TeamBlockMovableArea>            BlockMovableAreaFromEntity;

			private float MoveTowards(float current, float target, float maxDelta)
			{
				if (math.abs(target - current) <= maxDelta)
					return target;
				return current + Mathf.Sign(target - current) * maxDelta;
			}

			public void Execute(Entity                             entity,          int             jobIndex,
			                    ref            UnitControllerState controllerState, ref GroundState groundState,
			                    ref            Translation         translation,     ref Velocity    velocity,
			                    [ReadOnly] ref UnitPlayState       unitPlayState)
			{
				if (velocity.Value.y > 0)
					groundState.Value = false;

				var previousPosition = translation.Value;
				var target = controllerState.OverrideTargetPosition || !RelativeTargetFromEntity.Exists(entity)
					? controllerState.TargetPosition
					: TranslationFromEntity[RelativeTargetFromEntity[entity].Target].Value.x;

				if (LivableHealthFromEntity.Exists(entity) && LivableHealthFromEntity[entity].IsDead)
				{
					controllerState.ControlOverVelocity.x = true;
					if (groundState.Value)
						velocity.Value.x = math.lerp(velocity.Value.x, 0, 2.5f * DeltaTime);
				}
				
				if (!controllerState.ControlOverVelocity.x)
				{
					if (groundState.Value)
					{
						var speed = math.lerp(math.abs(velocity.Value.x), unitPlayState.MovementAttackSpeed, math.rcp(unitPlayState.Weight) * 30 * DeltaTime);

						// Instead of just assigning the translation value here, we calculate the velocity between the new position and the previous position.
						var newPosX = MoveTowards(translation.Value.x, target, speed * DeltaTime);

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

				for (var v = 0; v != 3; v++)
					velocity.Value[v] = math.isnan(velocity.Value[v]) ? 0.0f : velocity.Value[v];


				translation.Value += velocity.Value * DeltaTime;
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
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				DeltaTime = World.GetExistingSystem<ServerSimulationSystemGroup>().UpdateDeltaTime,
				Gravity   = new float3(0, -20f, 0),

				LivableHealthFromEntity    = GetComponentDataFromEntity<LivableHealth>(true),
				RelativeTargetFromEntity   = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true),
				TranslationFromEntity      = GetComponentDataFromEntity<Translation>(true),
				RelativeTeamFromEntity     = GetComponentDataFromEntity<Relative<TeamDescription>>(true),
				BlockMovableAreaFromEntity = GetComponentDataFromEntity<TeamBlockMovableArea>(true),
				TeamEnemiesFromEntity      = GetBufferFromEntity<TeamEnemies>(true),
				UnitDirectionFromEntity    = GetComponentDataFromEntity<UnitDirection>(true)
			}.Schedule(this, inputDeps);
		}
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	[UpdateAfter(typeof(UnitPhysicsSystem))]
	public class UnitPhysicsAfterBlockUpdateSystem : JobComponentSystem
	{
		[BurstCompile]
		[RequireComponentTag(typeof(UnitDescription))]
		private struct Job : IJobForEachWithEntity<UnitControllerState, LivableHealth, Translation, TeamAgainstMovable, Relative<TeamDescription>>
		{
			public float  DeltaTime;
			public float3 Gravity;

			[ReadOnly] public ComponentDataFromEntity<UnitDirection>             UnitDirectionFromEntity;
			[ReadOnly] public ComponentDataFromEntity<Relative<TeamDescription>> RelativeTeamFromEntity;
			[ReadOnly] public BufferFromEntity<TeamEnemies>                      TeamEnemiesFromEntity;
			public            ComponentDataFromEntity<TeamBlockMovableArea>      BlockMovableAreaFromEntity;

			public void Execute(Entity                        entity, int jobIndex,
			                    ref UnitControllerState       controllerState,
			                    ref LivableHealth livableHealth,
			                    ref Translation               translation,
			                    ref TeamAgainstMovable        against,
			                    [ReadOnly] ref Relative<TeamDescription> relativeTeam)
			{
				if (livableHealth.IsDead)
					return;
				
				var previousPosition = controllerState.PreviousPosition;
				var previousTranslation = translation.Value;
				
				var unitDirection    = UnitDirectionFromEntity[entity];
				if (!controllerState.PassThroughEnemies && TeamEnemiesFromEntity.Exists(relativeTeam.Target))
				{
					var enemies = TeamEnemiesFromEntity[relativeTeam.Target];
					for (var i = 0; i != enemies.Length; i++)
					{
						if (!BlockMovableAreaFromEntity.Exists(enemies[i].Target))
							continue;

						var area = BlockMovableAreaFromEntity[enemies[i].Target];
						// If the new position is superior the area and the previous one inferior, teleport back to the area.
						var size = against.Size * 0.5f + against.Center;
						if (translation.Value.x + size > area.LeftX && unitDirection.IsRight)
						{
							translation.Value.x = area.LeftX - size;
						}

						if (translation.Value.x - size < area.RightX && unitDirection.IsLeft)
						{
							translation.Value.x = area.RightX + size;
						}

						// if it's inside...
						if (translation.Value.x + size > area.LeftX && translation.Value.x - size < area.RightX)
						{
							if (unitDirection.IsLeft)
								translation.Value = area.RightX + size;
							else if (unitDirection.IsRight)
								translation.Value = area.LeftX - size;
						}
					}
				}

				translation.Value.y = previousTranslation.y;
				translation.Value.z = previousTranslation.z;

				if (BlockMovableAreaFromEntity.Exists(relativeTeam.Target))
				{
					var blockMovableArea = BlockMovableAreaFromEntity[relativeTeam.Target];
					blockMovableArea.LeftX  = math.min(translation.Value.x - against.Size - against.Center, blockMovableArea.LeftX);
					blockMovableArea.RightX = math.max(translation.Value.x + against.Size + against.Center, blockMovableArea.RightX);
					
					BlockMovableAreaFromEntity[relativeTeam.Target] = blockMovableArea;
				}

				for (var v = 0; v != 3; v++)
					translation.Value[v] = math.isnan(translation.Value[v]) ? 0.0f : translation.Value[v];
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			for (var i = 0; i != 2; i++)
			{
				inputDeps = new Job
				{
					DeltaTime = World.GetExistingSystem<ServerSimulationSystemGroup>().UpdateDeltaTime,
					Gravity   = new float3(0, -20f, 0),

					RelativeTeamFromEntity     = GetComponentDataFromEntity<Relative<TeamDescription>>(true),
					BlockMovableAreaFromEntity = GetComponentDataFromEntity<TeamBlockMovableArea>(false),
					TeamEnemiesFromEntity      = GetBufferFromEntity<TeamEnemies>(true),
					UnitDirectionFromEntity    = GetComponentDataFromEntity<UnitDirection>(true)
				}.ScheduleSingle(this, inputDeps);
			}

			return inputDeps;
		}
	}
}