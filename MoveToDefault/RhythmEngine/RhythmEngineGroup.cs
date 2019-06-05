using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	public class RhythmEngineGroup : ComponentSystemGroup
	{
	}
}