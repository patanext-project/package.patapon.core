using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.CoreAbilities.Mixed.CTate
{
	public struct TaterazayEnergyFieldAbility : IComponentData
	{
		public float MinDistance, MaxDistance;

		public float GivenDamageReduction, GivenDefenseReal;

		//public GameEntity BuffEntity;

		public class Register : RegisterGameHostComponentData<TaterazayEnergyFieldAbility>
		{
		}
	}
}