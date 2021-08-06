using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Hideout
{
	public struct HideoutSquadIndex : IComponentData
	{
		public int Value;

		public HideoutSquadIndex(int value)
		{
			Value = value;
		}

		public class Register : RegisterGameHostComponentData<HideoutSquadIndex>
		{
		}
	}
}