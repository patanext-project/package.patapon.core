using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(VersusHeadOnRuleGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[UpdateAfter(typeof(VersusHeadOnEliminationRule))] // this order is important, or else the respawned unit will be immediately dead again...
	[AlwaysSynchronizeSystem]
	public class VersusHeadOnRespawnUnits : RuleBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
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

			return default;
		}
	}
}