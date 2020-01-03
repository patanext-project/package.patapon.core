using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(GameEventRuleSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[AlwaysSynchronizeSystem]
	public class VersusHeadOnEliminationRuleSystem : RuleBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var eliminationEvents = World.GetExistingSystem<MpVersusHeadOnGameMode>().EliminationEvents;
			Entities.ForEach((Entity entity, ref LivableHealth health, in DynamicBuffer<HealthModifyingHistory> healthHistory, in VersusHeadOnUnit gmUnit) =>
			{
				if (!health.ShouldBeDead() || health.IsDead)
					return;

				health.IsDead = true;

				Entity lastInstigator = default;
				for (var i = 0; i != healthHistory.Length; i++)
				{
					if (healthHistory[i].Instigator != default && healthHistory[i].Value < 0)
						lastInstigator = healthHistory[i].Instigator;
				}

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
	}
}