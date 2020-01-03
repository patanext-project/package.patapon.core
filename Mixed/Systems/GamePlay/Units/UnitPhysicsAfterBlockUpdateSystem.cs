using Patapon.Mixed.GamePlay.Team;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Patapon.Mixed.GamePlay
{
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

			public void Execute(Entity                                   entity, int jobIndex,
			                    ref            UnitControllerState       controllerState,
			                    ref            LivableHealth             livableHealth,
			                    ref            Translation               translation,
			                    ref            TeamAgainstMovable        against,
			                    [ReadOnly] ref Relative<TeamDescription> relativeTeam)
			{
				if (livableHealth.IsDead)
					return;

				var previousPosition    = controllerState.PreviousPosition;
				var previousTranslation = translation.Value;

				var unitDirection = UnitDirectionFromEntity[entity];
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
					DeltaTime = Time.DeltaTime,
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