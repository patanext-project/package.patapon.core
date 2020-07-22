using Unity.Entities;

namespace Patapon.Client.OrderSystems.Vfx
{
	[UpdateInGroup(typeof(OrderSystemGroup))]
	[UpdateAfter(typeof(InGameVfxOrderingSystem.Begin))]
	[UpdateBefore(typeof(InGameVfxOrderingSystem.End))]
	public class InGameVfxOrderingSystem : OrderingSystem
	{
		[UpdateInGroup(typeof(OrderSystemGroup))]
		public class Begin : OrderingSystem
		{
			protected override void OnUpdate()
			{
				
			}
		}
		
		[UpdateInGroup(typeof(OrderSystemGroup))]
		public class End : OrderingSystem
		{
			protected override void OnUpdate()
			{
				
			}
		}
	}
}