using Runtime.Systems;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostSpawnSystemGroup))]
	[UpdateAfter(typeof(BeforeSimulationInterpolationSystem))]
	[UpdateAfter(typeof(ClientSimulationSystemGroup))]
	public class UpdateGhostSystemGroup : ComponentSystemGroup

	{

	}
}