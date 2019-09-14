using Unity.Entities;

namespace Patapon4TLB.Default
{
	public struct RhythmPredictedProcess : IComponentData
	{
		public int Beat;
		public int Time;
	}
}