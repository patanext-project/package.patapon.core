using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Patapon.Mixed.GamePlay.Team
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	public class TeamBlockMovableAreaSystem : JobComponentSystem
	{
		private struct CleanJob : IJobForEach<TeamBlockMovableArea>
		{
			public void Execute(ref TeamBlockMovableArea data)
			{
				data.NeedUpdate = true;
				data.LeftX      = float.PositiveInfinity;
				data.RightX     = float.NegativeInfinity;
			}
		}

		[BurstCompile]
		private struct Job : IJobForEachWithEntity<Translation, TeamAgainstMovable, Relative<TeamDescription>>
		{
			public ComponentDataFromEntity<TeamBlockMovableArea> BlockMovableAreaFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<LivableHealth> LivableHealthFromEntity;

			public void Execute(Entity entity, int index, ref Translation translation, ref TeamAgainstMovable ag, [ReadOnly] ref Relative<TeamDescription> teamRelative)
			{
				if (ag.Ignore)
					return;

				if (LivableHealthFromEntity.Exists(entity) && LivableHealthFromEntity[entity].IsDead)
					return;

				if (!BlockMovableAreaFromEntity.Exists(teamRelative.Target))
					return;

				var data = BlockMovableAreaFromEntity[teamRelative.Target];
				if (data.NeedUpdate)
				{
					data.NeedUpdate = false;
					data.LeftX      = translation.Value.x - ag.Size - ag.Center;
					data.RightX     = translation.Value.x + ag.Size + ag.Center;

					BlockMovableAreaFromEntity[teamRelative.Target] = data;

					return;
				}

				data.LeftX  = math.min(translation.Value.x - ag.Size - ag.Center, data.LeftX);
				data.RightX = math.max(translation.Value.x + ag.Size + ag.Center, data.RightX);

				BlockMovableAreaFromEntity[teamRelative.Target] = data;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new CleanJob().Schedule(this, inputDeps);
			inputDeps = new Job
			{
				LivableHealthFromEntity    = GetComponentDataFromEntity<LivableHealth>(),
				BlockMovableAreaFromEntity = GetComponentDataFromEntity<TeamBlockMovableArea>()
			}.ScheduleSingle(this, inputDeps);
			return inputDeps;
		}
	}
}