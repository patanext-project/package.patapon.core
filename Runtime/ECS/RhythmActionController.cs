using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct RhythmActionController : IComponentData
	{
		public Entity CurrentCommand;
	}
}