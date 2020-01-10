using StormiumTeam.GameBase.Systems;
using Unity.Entities;

namespace Patapon.Client.OrderSystems
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(UIGameModeOrderingSystem.Begin))]
	[UpdateBefore(typeof(UIGameModeOrderingSystem.End))]
	public class UIGameModeOrderingSystem : OrderingSystem
	{
		public class Begin : OrderingSystem
		{
		}

		public class End : OrderingSystem
		{
		}
	}
}