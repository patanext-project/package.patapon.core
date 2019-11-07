using Unity.Entities;

namespace Patapon.Mixed.RhythmEngine.Flow
{
	public struct FlowBeat : IComponentData
	{
		public int Beat;

		public FlowBeat(int beat)
		{
			Beat = beat;
		}
	}
}