using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.CoreAbilities.Mixed.CGuard
{
	public struct GuardiraBasicDefendAbility : IComponentData
	{
		public class Register : RegisterGameHostComponentData<GuardiraBasicDefendAbility>
		{
			
		}
	}
}