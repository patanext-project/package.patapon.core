using System;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Rules
{
	[UpdateInGroup(typeof(GameEventRuleSystemGroup))]
	[UpdateBefore(typeof(DefaultDamageRule))]
	public class ApplyDefensiveBonusToDamageRuleSystem : RuleBaseSystem
	{
		public override string Description => "Reduce damage with unit defense...";

		protected override void OnUpdate()
		{
			var playstateFromEntity = GetComponentDataFromEntity<UnitPlayState>(true);
			Entities
				.ForEach((ref TargetDamageEvent damageEvent) =>
				{
					if (damageEvent.Damage > 0 || damageEvent.Destination == default)
						return;
					if (!playstateFromEntity.TryGet(damageEvent.Destination, out var playState))
						return;

					var from = damageEvent.Damage;
					damageEvent.Damage = math.min(damageEvent.Damage + playState.Defense, 0);
					if (damageEvent.Damage != 0 && math.abs(playState.ReceiveDamagePercentage - 1) > 0.01f)
						damageEvent.Damage = (int) (damageEvent.Damage * playState.ReceiveDamagePercentage);
					Debug.Log($"from {from} to {damageEvent.Damage}");
				})
				.WithReadOnly(playstateFromEntity)
				.Run();
		}
	}
}