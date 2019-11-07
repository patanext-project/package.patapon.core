using Unity.Entities;

namespace Patapon.Mixed.GamePlay.Team
{
	public struct TeamBlockMovableArea : IComponentData
	{
		internal bool  NeedUpdate;
		public   float LeftX;
		public   float RightX;
	}
}