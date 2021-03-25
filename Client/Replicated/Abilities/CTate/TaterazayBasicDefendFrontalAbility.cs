using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.CoreAbilities.Mixed.CTate
{
	public struct TaterazayBasicDefendFrontalAbility : IComponentData
	{
		public float Range;
		
		public class Register : RegisterGameHostComponentData<TaterazayBasicDefendFrontalAbility>
		{
		}
	}
}