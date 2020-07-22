using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;

namespace PataNext.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIPlayerDisplayNameOrderSystem))]
	[UpdateAfter(typeof(UIPlayerDisplayAbilityOrderSystem))]
	[UpdateAfter(typeof(UIPlayerTargetCursorOrderSystem))]
	public class UIDrumPressureOrderingSystem : OrderingSystem
	{

	}
}