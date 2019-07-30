using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace P4.Core
{
	public struct TeamBlockMovableArea : IComponentData
	{
		internal bool  NeedUpdate;
		public   float LeftX;
		public   float RightX;
	}

	[UpdateInGroup(typeof(InitializationSystemGroup))]
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
		private struct Job : IJobForEach<Translation, TeamAgainstMovable, Relative<TeamDescription>>
		{
			public ComponentDataFromEntity<TeamBlockMovableArea> BlockMovableAreaFromEntity;

			public void Execute(ref Translation translation, ref TeamAgainstMovable ag, ref Relative<TeamDescription> teamRelative)
			{
				if (!BlockMovableAreaFromEntity.Exists(teamRelative.Target))
					return;

				var data = BlockMovableAreaFromEntity[teamRelative.Target];
				if (data.NeedUpdate)
				{
					data.NeedUpdate = false;
					data.LeftX      = translation.Value.x - ag.Size + ag.Center;
					data.RightX     = translation.Value.x + ag.Size + ag.Center;

					BlockMovableAreaFromEntity[teamRelative.Target] = data;

					return;
				}

				data.LeftX  = math.min(translation.Value.x - ag.Size + ag.Center, data.LeftX);
				data.RightX = math.max(translation.Value.x + ag.Size + ag.Center, data.RightX);

				BlockMovableAreaFromEntity[teamRelative.Target] = data;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new CleanJob().Schedule(this, inputDeps);
			inputDeps = new Job
			{
				BlockMovableAreaFromEntity = GetComponentDataFromEntity<TeamBlockMovableArea>()
			}.ScheduleSingle(this, inputDeps);
			return inputDeps;
		}
	}
}