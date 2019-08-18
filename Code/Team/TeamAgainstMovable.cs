using Unity.Entities;
using Unity.Mathematics;

namespace P4.Core
{
	public struct TeamAgainstMovable : IComponentData
	{
		public bool Ignore;
		
		public float Size;
		public float Center;
	}
}