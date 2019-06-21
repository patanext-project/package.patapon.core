using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct UnitBaseSettings : IComponentData
	{
		public float BaseWalkSpeed;
		public float FeverWalkSpeed;

		/// <summary>
		/// Weight can be used to calculate unit acceleration for moving or for knock-back power amplification.
		/// </summary>
		public float Weight;
	}
}