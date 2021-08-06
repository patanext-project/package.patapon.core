using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.GameModes
{
	public struct CoopMission : IComponentData
	{
		public class Register : RegisterGameHostComponentData<CoopMission>
		{
		}
	}
}