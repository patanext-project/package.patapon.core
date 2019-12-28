using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Revolution;
using Unity.Entities;

namespace SnapshotArchetypes
{
	[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
	public class SetRhythmEngineArchetypeSystem : ComponentSystem
	{
		public struct IsSet : IComponentData
		{
		}

		private EntityQuery m_RhythmEngineWithoutArchetype;

		protected override void OnCreate()
		{
			m_RhythmEngineWithoutArchetype = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(RhythmEngineDescription), typeof(FlowEngineProcess)},
				None = new ComponentType[] {typeof(IsSet)}
			});
		}

		protected override void OnUpdate()
		{
			EntityManager.AddComponent(m_RhythmEngineWithoutArchetype, typeof(RhythmEngineCommandProgression));
			EntityManager.AddComponent(m_RhythmEngineWithoutArchetype, typeof(GamePredictedCommandState));
			EntityManager.AddComponent(m_RhythmEngineWithoutArchetype, typeof(GameComboPredictedClient));
			EntityManager.AddComponent(m_RhythmEngineWithoutArchetype, typeof(IsSet));
		}
	}
}