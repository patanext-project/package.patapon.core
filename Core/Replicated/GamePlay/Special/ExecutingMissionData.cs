using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Client.Systems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.Special
{
	public struct ExecutingMissionData : IComponentData
	{
		public DentEntity Target;

		public ExecutingMissionData(DentEntity target)
		{
			Target = target;
		}

		public class Register : RegisterGameHostComponentData<ExecutingMissionData>
		{
		}
	}
}