using Unity.Entities;
using Unity.Mathematics;

namespace P4.Core
{
	public struct TeamAgainstMovable : IComponentData
	{
		public float Size;
		public float Center;
	}
}