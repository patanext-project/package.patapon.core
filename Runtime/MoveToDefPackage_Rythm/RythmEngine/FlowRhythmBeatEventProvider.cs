using package.patapon.def.Data;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace package.patapon.core
{
	public class FlowRhythmBeatEventProvider : BaseProviderBatch<FlowRhythmBeatEventProvider.Create>
	{
		public struct Create
		{
			public Entity Target;
			public int    FrameCount;
			public int    Beat;
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			new RhythmEventDestroySystem<FlowRhythmBeatData>(World);
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<RhythmShardEvent>(),
				ComponentType.ReadWrite<RhythmShardTarget>(),
				ComponentType.ReadWrite<RhythmBeatData>(),
				ComponentType.ReadWrite<FlowRhythmBeatData>()
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new RhythmShardEvent(data.FrameCount));
			EntityManager.SetComponentData(entity, new RhythmShardTarget(data.Target));
			EntityManager.SetComponentData(entity, new FlowRhythmBeatData(data.Beat));
		}
	}
}