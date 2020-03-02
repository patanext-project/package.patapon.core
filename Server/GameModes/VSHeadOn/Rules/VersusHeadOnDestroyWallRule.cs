using System;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public struct VersusHeadOnDestroyAreaRule : IComponentData
	{
		public int TowerReward;
		public int WallReward;
		public int TowerControlReward;
	}

	[UpdateInGroup(typeof(VersusHeadOnRuleGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[AlwaysSynchronizeSystem]
	public class VersusHeadOnDestroyAreaRuleSystem : RuleBaseSystem<VersusHeadOnDestroyAreaRule>
	{
		protected override void OnUpdate()
		{
			var rule                   = GetSingleton<VersusHeadOnDestroyAreaRule>();
			var tick                   = ServerTick;
			var destroyEvents          = World.GetExistingSystem<MpVersusHeadOnGameMode>().DestroyAreaEvents;
			var teamRelativeFromEntity = GetComponentDataFromEntity<Relative<TeamDescription>>(true);
			Entities.ForEach((Entity entity, ref LivableHealth health, in HeadOnStructure structure, in DynamicBuffer<HealthModifyingHistory> healthHistory) =>
			{
				if (!teamRelativeFromEntity.TryGet(entity, out var teamRelative))
					return;

				if (!health.ShouldBeDead() || health.IsDead)
					return;

				health.IsDead = true;

				Entity lastInstigator = default;
				for (var i = 0; i != healthHistory.Length; i++)
					if (healthHistory[i].Instigator != default && healthHistory[i].Value < 0)
						lastInstigator = healthHistory[i].Instigator;

				teamRelativeFromEntity.TryGet(lastInstigator, out var instigatorTeam);

				var score = 1;
				switch (structure.ScoreType)
				{
					case HeadOnStructure.EScoreType.TowerControl:
						score = rule.TowerControlReward;
						break;
					case HeadOnStructure.EScoreType.Tower:
						score = rule.TowerReward;
						break;
					case HeadOnStructure.EScoreType.Wall:
						score = rule.WallReward;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				destroyEvents.Add(new HeadOnOnDestroyArea
				{
					InstigatorTeam = instigatorTeam.Target,
					EntityTeam     = teamRelative.Target,

					Instigator = lastInstigator,
					Entity     = entity,

					Score = score
				});
			}).Run();
		}

		public RuleProperties<VersusHeadOnDestroyAreaRule>.Property<int> TowerReward;
		public RuleProperties<VersusHeadOnDestroyAreaRule>.Property<int> TowerControlReward;
		public RuleProperties<VersusHeadOnDestroyAreaRule>.Property<int> WallReward;

		protected override void AddRuleProperties()
		{
			TowerReward        = Rule.Add(d => d.TowerReward);
			TowerControlReward = Rule.Add(d => d.TowerControlReward);
			WallReward         = Rule.Add(d => d.WallReward);
		}

		protected override void SetDefaultProperties()
		{
			TowerReward.Value        = 25;
			TowerControlReward.Value = 50;
			WallReward.Value         = 10;
		}
	}
}