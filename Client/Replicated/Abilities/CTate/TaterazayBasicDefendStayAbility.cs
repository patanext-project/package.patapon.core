using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.CoreAbilities.Mixed.CTate
{
	public struct TaterazayBasicDefendStayAbility : IComponentData
	{
		public class Register : RegisterGameHostComponentData<TaterazayBasicDefendStayAbility>
		{
		}
	}
}