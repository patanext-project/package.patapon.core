using Runtime.Systems;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostSpawnSystemGroup))]
	[UpdateAfter(typeof(BeforeSimulationInterpolationSystem))]
	[UpdateBefore(typeof(ConvertGhostToOwnerSystem))]
	[UpdateBefore(typeof(ConvertGhostToRelativeSystemGroup))]
	public class UpdateGhostSystemGroup : ComponentSystemGroup

	{

	}
}