using package.patapon.def.Data;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace package.patapon.core
{
	public class FlowRhythmPressureEventProvider : BaseProviderBatch<FlowRhythmPressureEventProvider.Create>
	{
		public struct Create
		{
			public Entity Engine;
			public int    Key;
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
				ComponentType.ReadWrite<RhythmShardEvent>(),
				ComponentType.ReadWrite<RhythmShardTarget>(),
				ComponentType.ReadWrite<PressureEvent>(),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new RhythmShardEvent(0));
			EntityManager.SetComponentData(entity, new RhythmShardTarget(data.Engine));
			EntityManager.SetComponentData(entity, new PressureEvent {Engine = data.Engine, Key = data.Key});
		}
	}
}