using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Core
{
	[UpdateInGroup(typeof(GameEventRuleSystemGroup))]
	[UpdateBefore(typeof(DefaultDamageRule))]
	public class ApplyDefenseToDamageRule : RuleBaseSystem
	{
		public override string Name        => "Default Damage Rule";
		public override string Description => "Automatically manage the damage events.";

		[RequireComponentTag(typeof(GameEvent))]
		struct JobCreateEvents : IJobForEachWithEntity<TargetDamageEvent>
		{
			[ReadOnly]
			public ComponentDataFromEntity<UnitStatistics> StatisticsFromEntity;

			public void Execute(Entity entity, int index, ref TargetDamageEvent damageEvent)
			{
				if (damageEvent.Damage > 0)
					return;
				if (!StatisticsFromEntity.Exists(damageEvent.Destination))
					return;
				var statistics = StatisticsFromEntity[damageEvent.Destination];
				if (statistics.Defense <= 0)
					return;

				damageEvent.Damage += statistics.Defense;
				if (damageEvent.Damage > 0)
					damageEvent.Damage = 0;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new JobCreateEvents
			{
				StatisticsFromEntity = GetComponentDataFromEntity<UnitStatistics>()
			}.Schedule(this, inputDeps);

			return inputDeps;
		}
	}
}