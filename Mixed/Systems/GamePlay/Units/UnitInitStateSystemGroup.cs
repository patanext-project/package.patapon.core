using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;

namespace Patapon.Mixed.GamePlay
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[UpdateAfter(typeof(ActionSystemGroup))]
	public class UnitInitStateSystemGroup : ComponentSystemGroup
	{
	}
}