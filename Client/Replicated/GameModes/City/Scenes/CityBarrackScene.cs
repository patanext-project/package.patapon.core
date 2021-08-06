using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GameModes.City.Scenes
{
	public struct CityBarrackScene : IComponentData
	{
		public class Register : RegisterGameHostComponentData<CityBarrackScene>
		{
			
		}
	}
}