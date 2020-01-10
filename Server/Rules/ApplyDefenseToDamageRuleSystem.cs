using package.stormiumteam.shared.ecs;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Rules
{
	[UpdateInGroup(typeof(GameEventRuleSystemGroup))]
	public class ApplyDefenseToDamageRuleSystem : RuleBaseSystem
	{
		public override string Description => "Reduce damage with unit defense...";

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var statisticsFromEntity = GetComponentDataFromEntity<UnitStatistics>(true);
			inputDeps =
				Entities
					.ForEach((ref TargetDamageEvent damageEvent) =>
					{
						Debug.Log("Yes 1");
						if (damageEvent.Damage > 0 || damageEvent.Destination == default)
							return;
						Debug.Log("Yes 2");
						if (!statisticsFromEntity.TryGet(damageEvent.Destination, out var statistics))
							return;

						Debug.Log("Yes 3");
						damageEvent.Damage = math.min(damageEvent.Damage + statistics.Defense, 0);
					})
					.WithReadOnly(statisticsFromEntity)
					.Schedule(inputDeps);

			return inputDeps;
		}
	}
}