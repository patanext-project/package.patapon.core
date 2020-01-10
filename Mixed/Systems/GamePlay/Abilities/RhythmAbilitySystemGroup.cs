using Patapon.Mixed.Rules;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class RhythmAbilitySystemGroup : ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
			var parent = World.GetExistingSystem<ActionSystemGroup>();
			if (parent.isServer || !HasSingleton<P4NetworkRules.Data>())
			{
				base.OnUpdate();
			}
			else
			{
				var networkRule = GetSingleton<P4NetworkRules.Data>();
				if (networkRule.AbilityUsePredicted)
				{
					base.OnUpdate();
				}
			}
		}
	}
}