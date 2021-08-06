using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Army
{
	public struct UnitIndexInSquad : IComponentData
	{
		public int Value;
		
		public class Register : RegisterGameHostComponentData<UnitIndexInSquad> {}
	}
}