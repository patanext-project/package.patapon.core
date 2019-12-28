using Unity.Entities;

namespace Patapon.Mixed.RhythmEngine.Flow
{
	public struct FlowBeatEvent : IComponentData
	{
		public int Beat;

		public FlowBeatEvent(int beat)
		{
			Beat = beat;
		}
	}
}