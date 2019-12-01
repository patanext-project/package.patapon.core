using Patapon4TLB.Default;
using Unity.Collections;
using Unity.Entities;

namespace Patapon.Mixed.Units
{
	public struct UnitStatistics : IComponentData
	{
		public int Health;

		public int Attack;
		public float AttackSpeed;

		public int Defense;
		
		public float MovementAttackSpeed;
		public float BaseWalkSpeed;
		public float FeverWalkSpeed;
		/// <summary>
		/// Weight can be used to calculate unit acceleration for moving or for knock-back power amplification.
		/// </summary>
		public float Weight;

		public float AttackSeekRange;
	}

	public struct UnitStatusEffectStatistics : IBufferElementData
	{
		public StatusEffect Type;
		public int          CustomType; // this value would be set only if 'Type' is set to Unknown

		public float Value;
		public float RegenPerSecond;
	}

	public struct UnitPlayState : IComponentData
	{
		public float MovementSpeed;
		public float MovementAttackSpeed;
		public float AttackSpeed;

		public float Weight; // not used for now
	}

	public struct UnitDefinedAbilities : IBufferElementData
	{
		public NativeString512 Type;
		public int             Level;

		public UnitDefinedAbilities(string type, int level)
		{
			Type = new NativeString512(type);
			Level = level;
		}
	}
}