using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;

namespace PataNext.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderInterfaceSystemGroup))]
	[UpdateAfter(typeof(FeverWormOrderingSystem))]
	public class RhythmEngineBeatFrameOrdering : OrderingSystem
	{
	}
}