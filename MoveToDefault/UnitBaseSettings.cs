using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct UnitBaseSettings : IComponentData
	{
		public float MovementAttackSpeed;
		public float BaseWalkSpeed;
		public float FeverWalkSpeed;

		public float AttackSpeed;

		/// <summary>
		/// Weight can be used to calculate unit acceleration for moving or for knock-back power amplification.
		/// </summary>
		public float Weight;
	}
}