using Unity.Entities;

namespace Patapon.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIPlayerDisplayNameOrderSystem))]
	public class UIPlayerDisplayAbilityOrderSystem : OrderingSystem
	{
		
	}
}