using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;

namespace PataNext.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIGameModeOrderingSystem))]
	[UpdateBefore(typeof(UIGameModeOrderingSystem.End))]
	public class UIHeadOnInterfaceOrderSystem : OrderingSystem
	{

	}
}