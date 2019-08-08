using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct UnitStatistics : IComponentData
	{
		public int Health;

		public float MovementAttackSpeed;
		public float BaseWalkSpeed;
		public float FeverWalkSpeed;

		public float AttackSpeed;

		/// <summary>
		/// Weight can be used to calculate unit acceleration for moving or for knock-back power amplification.
		/// </summary>
		public float Weight;
	}

	public struct UnitPlayState : IComponentData
	{
		public float MovementSpeed;
		public float MovementAttackSpeed;
		public float AttackSpeed;

		public float Weight; // not used for now
	}
}