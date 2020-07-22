using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;

namespace PataNext.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIPlayerTargetCursorOrderSystem))]
	public class UIPlayerDisplayNameOrderSystem : OrderingSystem
	{
		
	}
}