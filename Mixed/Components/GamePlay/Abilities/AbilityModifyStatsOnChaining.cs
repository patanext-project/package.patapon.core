using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public struct AbilityModifyStatsOnChaining : IComponentData
	{
		public StatisticModifier ActiveModifier;
		public StatisticModifier FeverModifier;
		public StatisticModifier PerfectModifier;

		public StatisticModifier ChargeModifier;
		public bool              SetChargeModifierAsFirst;
	}
}