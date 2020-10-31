using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Simulation.Mixed.Abilities.CTate
{
	public struct TaterazayBasicDefendStayAbility : IComponentData
	{
		public class Register : RegisterGameHostComponentData<TaterazayBasicDefendStayAbility>
		{
		}
	}
}