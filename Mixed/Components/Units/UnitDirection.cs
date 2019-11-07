using Unity.Entities;

namespace Patapon.Mixed.Units
{
	public struct UnitDirection : IComponentData
	{
		public static readonly UnitDirection Right = new UnitDirection {Value = 1};
		public static readonly UnitDirection Left  = new UnitDirection {Value = -1};

		public sbyte Value;

		public bool IsLeft  => Value == -1;
		public bool IsRight => Value == 1;

		public bool Invalid => !IsLeft && !IsRight;
	}
}