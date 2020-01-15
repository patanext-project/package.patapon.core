using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon.Mixed.GamePlay
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	[UpdateBefore(typeof(ActionSystemGroup))]
	public class UnitInitStateSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	[UpdateAfter(typeof(ActionSystemGroup))]
	public class UnitPhysicSystemGroup : BaseGhostPredictionSystemGroup
	{
	}
}