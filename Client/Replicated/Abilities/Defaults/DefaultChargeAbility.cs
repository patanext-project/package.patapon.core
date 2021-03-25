using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.CoreAbilities.Mixed.Defaults
{
	public struct DefaultChargeAbility : IComponentData
	{
		public class Register : RegisterGameHostComponentData<DefaultChargeAbility>
		{
		}
	}
}