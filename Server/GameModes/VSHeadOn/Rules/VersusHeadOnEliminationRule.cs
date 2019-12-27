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
	public class VersusHeadOnEliminationRuleSystem : RuleBaseSystem
	{
		private struct UpdateJob : IJobForEachWithEntity_EBCC<HealthModifyingHistory, LivableHealth, VersusHeadOnUnit>
		{
			public Entity                                               GameModeEntity;
			public NativeList<HeadOnOnUnitElimination> EliminationEvents;

			public void Execute(Entity entity, int index, DynamicBuffer<HealthModifyingHistory> healthHistory, ref LivableHealth health, ref VersusHeadOnUnit gmUnit)
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

				EliminationEvents.Add(new HeadOnOnUnitElimination
				{
					InstigatorTeam = 1 - gmUnit.Team,
					EntityTeam     = gmUnit.Team,

					Instigator = lastInstigator,
					Entity     = entity
				});
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new UpdateJob
			{
				GameModeEntity    = default,
				EliminationEvents = World.GetExistingSystem<MpVersusHeadOnGameMode>().EliminationEvents
			}.ScheduleSingle(this, inputDeps);
		}
	}
}