using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Simulation.Mixed.Abilities.Defaults
{
	public struct DefaultMarchAbility : IComponentData
	{
		public class Register : RegisterGameHostComponentData<DefaultMarchAbility>
		{}
	}
}