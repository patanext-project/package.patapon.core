using package.StormiumTeam.GameBase;
using Patapon4TLB.Default;
using Patapon4TLB.UI.InGame;
using Runtime.Systems;
using StormiumTeam.GameBase;
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
		public float3 Position;
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

				typeof(UnitBaseSettings),
				typeof(UnitControllerState),
				typeof(UnitDirection),
				typeof(UnitTargetPosition),
				typeof(UnitRhythmState),

				typeof(Translation),
				typeof(Rotation),
				//typeof(LocalToWorld),

				typeof(PhysicsCollider),
				typeof(PhysicsDamping),
				typeof(PhysicsMass),
				typeof(Velocity),
				typeof(GroundState),

				typeof(GhostOwner),
				typeof(Owner),

				typeof(GhostRelative<PlayerDescription>),
				typeof(Relative<PlayerDescription>),
				typeof(GhostRelative<RhythmEngineDescription>),
				typeof(Relative<RhythmEngineDescription>),
				typeof(GhostRelative<TeamDescription>),
				typeof(Relative<TeamDescription>),

				typeof(ActionContainer),

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

				typeof(UnitBaseSettings),
				typeof(UnitControllerState),
				typeof(UnitDirection),
				typeof(UnitTargetPosition),
				typeof(UnitRhythmState),

				typeof(Translation),
				typeof(Rotation),
				typeof(LocalToWorld),

				typeof(PhysicsCollider),
				typeof(PhysicsDamping),
				typeof(PhysicsMass),
				typeof(Velocity),
				typeof(GroundState),

				typeof(GhostRelative<PlayerDescription>),
				typeof(Relative<PlayerDescription>),
				typeof(GhostRelative<RhythmEngineDescription>),
				typeof(Relative<RhythmEngineDescription>),
				typeof(GhostRelative<TeamDescription>),
				typeof(Relative<TeamDescription>),

				typeof(ActionContainer),

				typeof(ReplicatedEntityComponent),
				typeof(PredictedEntityComponent)
			);
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	[UpdateAfter(typeof(GhostSpawnSystemGroup))]
	[UpdateAfter(typeof(BeforeSimulationInterpolationSystem))]
	[UpdateBefore(typeof(ConvertGhostToOwnerSystem))]
	[UpdateBefore(typeof(ConvertGhostToRelativeSystemGroup))]
	public class BasicUnitUpdateSystem : JobComponentSystem
	{
		private struct Job : IJobForEachWithEntity<UnitDirection, BasicUnitSnapshotTarget, Translation, Velocity>
		{
			public uint InterpolateTick;
			public uint PredictTick;

			[ReadOnly] public BufferFromEntity<BasicUnitSnapshotData> SnapshotDataFromEntity;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GhostRelative<PlayerDescription>>       RelativePlayerFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GhostRelative<TeamDescription>>         RelativeTeamFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GhostRelative<RhythmEngineDescription>> RelativeRhythmEngineFromEntity;

			public void Execute(Entity entity, int jobIndex, ref UnitDirection unitDirection, ref BasicUnitSnapshotTarget target, ref Translation translation, ref Velocity velocity)
			{
				SnapshotDataFromEntity[entity].GetDataAtTick(PredictTick, out var snapshot);

				unitDirection.Value = (sbyte) snapshot.Direction;
				velocity.Value      = snapshot.Velocity.Get(BasicUnitSnapshotData.DeQuantization);

				var targetPosition   = snapshot.Position.Get(BasicUnitSnapshotData.DeQuantization);

				target.Position = targetPosition;


				RelativePlayerFromEntity[entity] = new GhostRelative<PlayerDescription>
				{
					GhostId = (int) snapshot.PlayerGhostId
				};
				RelativeTeamFromEntity[entity] = new GhostRelative<TeamDescription>
				{
					GhostId = (int) snapshot.TeamGhostId
				};
				RelativeRhythmEngineFromEntity[entity] = new GhostRelative<RhythmEngineDescription>
				{
					GhostId = (int) snapshot.RhythmEngineGhostId
				};
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				InterpolateTick = NetworkTimeSystem.interpolateTargetTick,
				PredictTick = NetworkTimeSystem.predictTargetTick,

				SnapshotDataFromEntity = GetBufferFromEntity<BasicUnitSnapshotData>(true),

				RelativePlayerFromEntity       = GetComponentDataFromEntity<GhostRelative<PlayerDescription>>(),
				RelativeTeamFromEntity         = GetComponentDataFromEntity<GhostRelative<TeamDescription>>(),
				RelativeRhythmEngineFromEntity = GetComponentDataFromEntity<GhostRelative<RhythmEngineDescription>>(),
			}.Schedule(this, inputDeps);
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
				var distance = math.distance(translation.Value, target.Position);
				
				translation.Value = math.lerp(translation.Value, target.Position, Time.deltaTime * (velocity.speed + distance + 1f));
				translation.Value = Vector3.MoveTowards(translation.Value, target.Position, Time.deltaTime * math.max(distance, velocity.speed) * 0.9f);
			});
		}
	}
}