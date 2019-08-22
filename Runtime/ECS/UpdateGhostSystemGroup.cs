using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostSpawnSystemGroup))]
	[UpdateAfter(typeof(BeforeSimulationInterpolationSystem))]
	[UpdateAfter(typeof(PreConvertSystemGroup))]
	[UpdateBefore(typeof(PostConvertSystemGroup))]
	public class UpdateGhostSystemGroup : ComponentSystemGroup
	{

	}
}