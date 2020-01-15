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
	public class ApplyDefensiveBonusToDamageRuleSystem : RuleBaseSystem
	{
		public override string Description => "Reduce damage with unit defense...";

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var playstateFromEntity = GetComponentDataFromEntity<UnitPlayState>(true);
			inputDeps =
				Entities
					.ForEach((ref TargetDamageEvent damageEvent) =>
					{
						if (damageEvent.Damage > 0 || damageEvent.Destination == default)
							return;
						if (!playstateFromEntity.TryGet(damageEvent.Destination, out var playState))
							return;
						damageEvent.Damage = math.min(damageEvent.Damage + playState.Defense, 0);
						damageEvent.Damage = (int) (damageEvent.Damage * playState.ReceiveDamagePercentage);
					})
					.WithReadOnly(playstateFromEntity)
					.Schedule(inputDeps);

			return inputDeps;
		}
	}
}