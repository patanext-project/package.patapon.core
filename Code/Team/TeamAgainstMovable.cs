using Unity.Entities;

namespace Patapon4TLBCore
{
	public struct TeamAgainstMovable : IComponentData
	{
		public bool Ignore;
		
		public float Size;
		public float Center;
	}
}