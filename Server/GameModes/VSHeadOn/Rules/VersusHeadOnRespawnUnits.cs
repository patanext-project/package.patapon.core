using Systems.GamePlay;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(VersusHeadOnRuleGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[UpdateAfter(typeof(VersusHeadOnEliminationRuleSystem))] // this order is important, or else the respawned unit will be immediately dead again...
	[AlwaysSynchronizeSystem]
	public class VersusHeadOnRespawnUnits : RuleBaseSystem
	{
		protected override void OnUpdate()
		{
			var tick          = ServerTick;
			var respawnEvents = World.GetExistingSystem<MpVersusHeadOnGameMode>().RespawnEvents;
			Entities.ForEach((Entity entity, ref LivableHealth health, ref VersusHeadOnUnit gmUnit) =>
			{
				if (health.IsDead && gmUnit.TickBeforeSpawn <= tick.Value)
				{
					health.IsDead = false;
					respawnEvents.Add(entity);
				}
			}).Run();

			var livableHealthFromEntity = GetComponentDataFromEntity<LivableHealth>();
			var gameModeUnitFromEntity  = GetComponentDataFromEntity<VersusHeadOnUnit>();
			Entities.ForEach((ref TargetRebornEvent rebornEvent) =>
			{
				var gmUnitUpdater = gameModeUnitFromEntity.GetUpdater(rebornEvent.Target)
				                                          .Out(out var gmUnit);
				if (!gmUnitUpdater.possess || !(livableHealthFromEntity[rebornEvent.Target].IsDead || livableHealthFromEntity[rebornEvent.Target].ShouldBeDead()))
					return;

				gmUnit.TickBeforeSpawn = 0;
				gmUnitUpdater.CompareAndUpdate(gmUnit);
				respawnEvents.Add(rebornEvent.Target);
			}).Run();
		}
	}
}