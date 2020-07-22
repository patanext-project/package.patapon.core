using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;

namespace PataNext.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIPlayerDisplayNameOrderSystem))]
	public class UIPlayerDisplayAbilityOrderSystem : OrderingSystem
	{
		
	}
}