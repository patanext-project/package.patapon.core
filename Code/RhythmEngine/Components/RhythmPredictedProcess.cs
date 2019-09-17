using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct RhythmPredictedProcess : IComponentData
	{
		public int Beat;
		public int Time;
	}
}