using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace package.patapon.core
{
	public class FlowRhythmPressureEventProvider : BaseProviderBatch<FlowRhythmPressureEventProvider.Create>
	{
		public struct Create
		{
			public PressureEvent Ev;
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			new RhythmEventDestroySystem<PressureEvent>(World);
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<PressureEvent>()
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, data.Ev);
		}
	}
}