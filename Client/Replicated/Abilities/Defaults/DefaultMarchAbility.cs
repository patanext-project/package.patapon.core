using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.CoreAbilities.Mixed.Defaults
{
	public struct DefaultMarchAbility : IComponentData
	{
		public class Register : RegisterGameHostComponentData<DefaultMarchAbility>
		{ }
	}
}