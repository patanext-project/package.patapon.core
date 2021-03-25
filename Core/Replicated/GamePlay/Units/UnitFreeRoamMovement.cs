using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.Units
{
	public struct UnitFreeRoamMovement : IComponentData
	{
		public class Register : RegisterGameHostComponentData<UnitFreeRoamMovement>
		{
		}
	}
}