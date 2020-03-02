using Patapon.Mixed.Units;
using Unity.Entities;

namespace Patapon.Mixed.GamePlay
{
	public struct DamageFrame : IComponentData
	{
		public int Damage;
	}

	public struct DamageFromStatisticFrame : IComponentData
	{
		public Entity            UseValueFrom;
		public UnitPlayState     Value;
		public StatisticModifier Modifier;
	}

	public struct DamageResultFrame : IComponentData
	{
		public UnitPlayState PlayState;
	}
}