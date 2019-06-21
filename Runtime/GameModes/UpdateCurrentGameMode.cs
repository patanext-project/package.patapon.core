using Unity.Entities;
using Unity.NetCode;

namespace Patapon4TLB.GameModes
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class UpdateCurrentGameMode : ComponentSystem
	{
		protected override void OnUpdate()
		{
			
		}
	}
}