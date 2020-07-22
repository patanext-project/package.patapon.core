using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;

namespace PataNext.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIGameModeOrderingSystem.Begin))]
	[UpdateBefore(typeof(UIGameModeOrderingSystem.End))]
	public class UIGameModeOrderingSystem : OrderingSystem
	{
		[UpdateInGroup(typeof(OrderSystemGroup))]
		public class Begin : OrderingSystem
		{
		}

		[UpdateInGroup(typeof(OrderSystemGroup))]
		public class End : OrderingSystem
		{
		}
	}
}