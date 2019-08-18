using Patapon4TLB.Core.BasicUnitSnapshot;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Patapon4TLB.Core
{
	public class UnitTargetGhostSpawnSystem : DefaultGhostSpawnSystem<UnitTargetSnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(UnitTargetDescription),
				typeof(UnitTargetSnapshotData),
				typeof(Translation),
				typeof(ReplicatedEntityComponent)
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(UnitTargetDescription),
				typeof(UnitTargetSnapshotData),
				typeof(Translation),
				typeof(ReplicatedEntityComponent),
				typeof(PredictedEntityComponent)
			);
		}
	}

	[UpdateInGroup(typeof(UpdateGhostSystemGroup))]
	public class UnitTargetUpdateSystem : JobComponentSystem
	{
		[RequireComponentTag(typeof(UnitTargetSnapshotData))]
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<Translation>
		{
			public uint PredictTick;

			[ReadOnly] public BufferFromEntity<UnitTargetSnapshotData> SnapshotDataFromEntity;

			public void Execute(Entity entity, int jobIndex, ref Translation translation)
			{
				SnapshotDataFromEntity[entity].GetDataAtTick(PredictTick, out var snapshot);

				translation.Value = snapshot.Position.Get(UnitTargetSnapshotData.DeQuantization);
			}
		}

		private NetworkTimeSystem m_NetworkTimeSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_NetworkTimeSystem = World.GetOrCreateSystem<NetworkTimeSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				PredictTick            = m_NetworkTimeSystem.predictTargetTick,
				SnapshotDataFromEntity = GetBufferFromEntity<UnitTargetSnapshotData>(true),
			}.Schedule(this, inputDeps);
		}
	}
}