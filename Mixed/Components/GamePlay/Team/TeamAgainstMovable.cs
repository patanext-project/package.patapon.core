using Unity.Entities;

namespace Patapon.Mixed.GamePlay.Team
{
	public struct TeamAgainstMovable : IComponentData
	{
		public bool Ignore;
		
		public float Size;
		public float Center;
	}
}