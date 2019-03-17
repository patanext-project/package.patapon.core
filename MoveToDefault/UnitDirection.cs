using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct UnitDirection : IComponentData
	{
		public sbyte Value;

		public bool IsLeft  => Value == -1;
		public bool IsRight => Value == 1;

		public bool Invalid => !IsLeft && !IsRight;
	}
}