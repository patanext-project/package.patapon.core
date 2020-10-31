using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;

namespace PataNext.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderInterfaceSystemGroup))]
	[UpdateAfter(typeof(UIGameModeOrderingSystem))]
	public class FeverWormOrderingSystem : OrderingSystem
	{
	}
}