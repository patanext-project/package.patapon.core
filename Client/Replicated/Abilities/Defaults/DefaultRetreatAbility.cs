using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.CoreAbilities.Mixed.Defaults
{
	public struct DefaultRetreatAbility : IComponentData
	{
		public const float StopTime      = 1.5f;
		public const float MaxActiveTime = StopTime + 0.5f;

		public int LastActiveId;

		public float AccelerationFactor;
		public float StartPosition;
		public float BackVelocity;
		public bool  IsRetreating;
		public float ActiveTime;

		public class Register : RegisterGameHostComponentData<DefaultRetreatAbility>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<DefaultRetreatAbility>();
		}
	}
}