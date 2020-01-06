using StormiumTeam.GameBase.Systems;
using Unity.Entities;

namespace Patapon.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderInterfaceSystemGroup))]
	[UpdateAfter(typeof(UIHeadOnInterfaceOrderSystem))]
	public class FeverWormOrderingSystem : OrderingSystem
	{
	}
}