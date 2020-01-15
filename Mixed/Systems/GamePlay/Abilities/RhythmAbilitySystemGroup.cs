using Patapon.Mixed.Rules;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class RhythmAbilitySystemGroup : ComponentSystemGroup
	{
		private ServerSimulationSystemGroup m_ServerGroup;

		public bool IsPredicted { get; private set; }

		protected override void OnCreate()
		{
			base.OnCreate();
			m_ServerGroup = World.GetExistingSystem<ServerSimulationSystemGroup>();
		}

		protected override void OnUpdate()
		{
			if (m_ServerGroup != null || !HasSingleton<P4NetworkRules.Data>())
			{
				IsPredicted = true;
				base.OnUpdate();
			}
			else
			{
				var networkRule = GetSingleton<P4NetworkRules.Data>();
				IsPredicted = networkRule.AbilityUsePredicted;
				base.OnUpdate();
			}
		}
	}
}