using Unity.Entities;
using Unity.NetCode;

namespace Patapon.Mixed.Systems
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[UpdateAfter(typeof(NetworkReceiveSnapshotSystemGroup))]
	[UpdateAfter(typeof(GhostSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostPredictionSystemGroup))]
	public class RhythmEngineGroup : ComponentSystemGroup
	{
		
	}
}