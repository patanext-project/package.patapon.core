using Unity.Entities;

namespace Patapon.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIPlayerTargetCursorOrderSystem))]
	public class UIPlayerDisplayNameOrderSystem : OrderingSystem
	{
		
	}
}