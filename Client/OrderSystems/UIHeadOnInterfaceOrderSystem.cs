using StormiumTeam.GameBase.Systems;
using Unity.Entities;

namespace Patapon.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIGameModeOrderingSystem))]
	[UpdateBefore(typeof(UIGameModeOrderingSystem.End))]
	public class UIHeadOnInterfaceOrderSystem : OrderingSystem
	{

	}
}