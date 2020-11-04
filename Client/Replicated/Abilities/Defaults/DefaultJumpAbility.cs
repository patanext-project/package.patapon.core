using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.CoreAbilities.Mixed.Defaults
{
	public struct DefaultJumpAbility : IComponentData
	{
		public int LastActiveId;

		public bool  IsJumping;
		public float ActiveTime;

		public class Register : RegisterGameHostComponentData<DefaultJumpAbility>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<DefaultJumpAbility>();
		}
	}
}