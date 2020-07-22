using Unity.Entities;

namespace Patapon.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIPlayerDisplayNameOrderSystem))]
	[UpdateAfter(typeof(UIPlayerDisplayAbilityOrderSystem))]
	[UpdateAfter(typeof(UIPlayerTargetCursorOrderSystem))]
	public class UIDrumPressureOrderingSystem : OrderingSystem
	{

	}
}