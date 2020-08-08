using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Simulation.Mixed.Abilities.Defaults
{
	public struct DefaultJumpAbility : IComponentData
	{
		public int LastActiveId;

		public bool  IsJumping;
		public float ActiveTime;

		public class Register : RegisterGameHostComponentData<DefaultJumpAbility>
		{
		}
	}
}