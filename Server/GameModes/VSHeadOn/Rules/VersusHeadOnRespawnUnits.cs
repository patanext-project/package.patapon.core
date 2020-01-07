using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(VersusHeadOnRuleGroup))]
	public class VersusHeadOnRespawnUnits : RuleBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			
		}
	}
}