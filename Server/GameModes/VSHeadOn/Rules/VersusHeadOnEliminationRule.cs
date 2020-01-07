using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public struct VersusHeadOnEliminationRule : IComponentData
	{
		public int InitialRespawnTime;
		public int IncreasePerRespawn;
		public int MaxRespawnTime;
	}

	[UpdateInGroup(typeof(GameEventRuleSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[AlwaysSynchronizeSystem]
	public class VersusHeadOnEliminationRuleSystem : RuleBaseSystem<VersusHeadOnEliminationRule>
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var rule              = GetSingleton<VersusHeadOnEliminationRule>();
			var tick              = ServerTick;
			var eliminationEvents = World.GetExistingSystem<MpVersusHeadOnGameMode>().EliminationEvents;
			Entities.ForEach((Entity entity, ref LivableHealth health, ref VersusHeadOnUnit gmUnit, in DynamicBuffer<HealthModifyingHistory> healthHistory) =>
			{
				if (!health.ShouldBeDead() || health.IsDead)
					return;

				health.IsDead = true;

				Entity lastInstigator = default;
				for (var i = 0; i != healthHistory.Length; i++)
					if (healthHistory[i].Instigator != default && healthHistory[i].Value < 0)
						lastInstigator = healthHistory[i].Instigator;

				var respawnTime = math.clamp(gmUnit.DeadCount * rule.IncreasePerRespawn + rule.InitialRespawnTime, rule.InitialRespawnTime, rule.MaxRespawnTime);
				gmUnit.TickBeforeSpawn = UTick.AddMsNextFrame(tick, respawnTime).Value;
				gmUnit.DeadCount++;

				eliminationEvents.Add(new HeadOnOnUnitElimination
				{
					InstigatorTeam = 1 - gmUnit.Team,
					EntityTeam     = gmUnit.Team,

					Instigator = lastInstigator,
					Entity     = entity
				});
			}).Run();

			return default;
		}

		public RuleProperties<VersusHeadOnEliminationRule>.Property<int> InitialRespawnTime;
		public RuleProperties<VersusHeadOnEliminationRule>.Property<int> IncreasePerRespawn;
		public RuleProperties<VersusHeadOnEliminationRule>.Property<int> MaxRespawnTime;

		protected override void AddRuleProperties()
		{
			InitialRespawnTime = Rule.Add(d => d.InitialRespawnTime);
			IncreasePerRespawn = Rule.Add(d => d.IncreasePerRespawn);
			MaxRespawnTime     = Rule.Add(d => d.MaxRespawnTime);
		}

		protected override void SetDefaultProperties()
		{
			InitialRespawnTime.Value = 10_000;
			IncreasePerRespawn.Value = 5_000;
			MaxRespawnTime.Value     = 20_000;
		}
	}
}