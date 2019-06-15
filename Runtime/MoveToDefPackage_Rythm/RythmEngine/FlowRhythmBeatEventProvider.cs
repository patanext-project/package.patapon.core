using package.patapon.def.Data;
using StormiumTeam.GameBase;
using Unity.Entities;

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

			new RhythmEventDestroySystem<RhythmBeatData>(World);
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<RhythmShardEvent>(),
				ComponentType.ReadWrite<RhythmShardTarget>(),
				ComponentType.ReadWrite<def.Data.RhythmBeatData>(),
				ComponentType.ReadWrite<RhythmBeatData>()
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.SetComponentData(entity, new RhythmShardEvent(data.FrameCount));
			EntityManager.SetComponentData(entity, new RhythmShardTarget(data.Target));
			EntityManager.SetComponentData(entity, new RhythmBeatData(data.Beat));
		}
	}
}