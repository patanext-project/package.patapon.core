using package.StormiumTeam.GameBase;
using Patapon4TLB.Default;
using Patapon4TLB.UI.InGame;
using Runtime.Systems;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Core.BasicUnitSnapshot
{
	public struct BasicUnitSnapshotTarget : IComponentData
	{
		public uint   LastSnapshotTick;
		public bool   Grounded;
		public float3 Position;
		public int    NearPositionCount;
	}

	public class BasicUnitGhostSpawnSystem : DefaultGhostSpawnSystem<BasicUnitSnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(BasicUnitSnapshotData),
				typeof(BasicUnitSnapshotTarget),
				typeof(UnitDescription),

				typeof(UnitStatistics),
				typeof(UnitPlayState),
				typeof(UnitControllerState),
				typeof(UnitDirection),
				typeof(UnitTargetOffset),
				
				typeof(Translation),
				typeof(Rotation),
				//typeof(LocalToWorld),

				typeof(PhysicsCollider),
				typeof(PhysicsDamping),
				typeof(PhysicsMass),
				typeof(Velocity),
				typeof(GroundState),
				
				typeof(LivableHealth),

				typeof(GhostOwner),
				typeof(Owner),

				typeof(ActionContainer),
				typeof(HealthContainer),
				
				typeof(ReplicatedEntityComponent)
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(BasicUnitSnapshotData),
				typeof(BasicUnitSnapshotTarget),
				typeof(UnitDescription),

				typeof(UnitStatistics),
				typeof(UnitControllerState),
				typeof(UnitDirection),

				typeof(Translation),
				typeof(Rotation),
				typeof(LocalToWorld),

				typeof(PhysicsCollider),
				typeof(PhysicsDamping),
				typeof(PhysicsMass),
				typeof(Velocity),
				typeof(GroundState),
				
				typeof(LivableHealth),
				
				typeof(ActionContainer),
				typeof(HealthContainer),

				typeof(ReplicatedEntityComponent),
				typeof(PredictedEntityComponent)
			);
		}
	}

	[UpdateInGroup(typeof(UpdateGhostSystemGroup))]
	public class BasicUnitUpdateSystem : JobComponentSystem
	{
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<UnitDirection, BasicUnitSnapshotTarget, Translation, Velocity>
		{
			public uint InterpolateTick;
			public uint PredictTick;

			[ReadOnly] public BufferFromEntity<BasicUnitSnapshotData> SnapshotDataFromEntity;
			[ReadOnly] public NativeHashMap<int, Entity>              GhostEntityMap;
			
			public void Execute(Entity entity, int jobIndex, ref UnitDirection unitDirection, ref BasicUnitSnapshotTarget target, ref Translation translation, ref Velocity velocity)
			{
				SnapshotDataFromEntity[entity].GetDataAtTick(PredictTick, out var snapshot);

				unitDirection.Value      = (sbyte) snapshot.Direction;
				velocity.Value           = snapshot.Velocity.Get(BasicUnitSnapshotData.DeQuantization);

				var targetPosition = snapshot.Position.Get(BasicUnitSnapshotData.DeQuantization);
				if (math.distance(target.Position, targetPosition) < 0.025f && target.LastSnapshotTick != snapshot.Tick)
				{
					target.NearPositionCount++;
				}
				else
				{
					target.NearPositionCount = 0;
				}

				target.LastSnapshotTick = snapshot.Tick;
				target.Position         = targetPosition;
				target.Grounded         = snapshot.GroundFlags == 1;
			}
		}

		private ConvertGhostEntityMap m_ConvertGhostEntityMap;
		private NetworkTimeSystem m_NetworkTimeSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ConvertGhostEntityMap = World.GetOrCreateSystem<ConvertGhostEntityMap>();
			m_NetworkTimeSystem = World.GetOrCreateSystem<NetworkTimeSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				InterpolateTick = m_NetworkTimeSystem.interpolateTargetTick,
				PredictTick     = m_NetworkTimeSystem.predictTargetTick,

				SnapshotDataFromEntity = GetBufferFromEntity<BasicUnitSnapshotData>(true),
				GhostEntityMap         = m_ConvertGhostEntityMap.HashMap,
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, m_ConvertGhostEntityMap.dependency));
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateBefore(typeof(ClientPresentationTransformSystemGroup))]
	public class BasicUnitUpdatePresentationSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((ref Translation translation, ref Velocity velocity, ref BasicUnitSnapshotTarget target) =>
			{
				for (var v = 0; v != 3; v++)
					translation.Value[v] = math.isnan(translation.Value[v]) ? 0.0f : translation.Value[v];
			
				var distance = math.distance(translation.Value, target.Position);
				var factor = target.NearPositionCount + 1;

				translation.Value = math.lerp(translation.Value, target.Position, Time.deltaTime * (velocity.speed + distance + 1f));
				translation.Value = Vector3.MoveTowards(translation.Value, target.Position, math.max(distance * 0.1f, velocity.speed * Time.deltaTime) * 0.9f * factor);
				if (target.Grounded)
				{
					translation.Value.y = 0;
				}
			});
			
			Entities.ForEach((UnitVisualBackend backend) =>
			{
				var unitDirection = EntityManager.GetComponentData<UnitDirection>(backend.DstEntity);
				
				backend.transform.localScale = new Vector3(unitDirection.Value, 1, 1);
			});
		}
	}
}