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
		public uint LastSnapshotTick;
		public bool Grounded;
		public float3 Position;
		public int NearPositionCount;
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

	[UpdateInGroup(typeof(UpdateGhostSystemGroup))]
	public class BasicUnitUpdateSystem : JobComponentSystem
	{
		private struct Job : IJobForEachWithEntity<UnitDirection, BasicUnitSnapshotTarget, UnitTargetPosition, Translation, Velocity>
		{
			public uint InterpolateTick;
			public uint PredictTick;

			[ReadOnly] public BufferFromEntity<BasicUnitSnapshotData> SnapshotDataFromEntity;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GhostRelative<PlayerDescription>>       RelativePlayerFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GhostRelative<TeamDescription>>         RelativeTeamFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GhostRelative<RhythmEngineDescription>> RelativeRhythmEngineFromEntity;

			public void Execute(Entity entity, int jobIndex, ref UnitDirection unitDirection, ref BasicUnitSnapshotTarget target, ref UnitTargetPosition unitTargetPosition, ref Translation translation, ref Velocity velocity)
			{
				SnapshotDataFromEntity[entity].GetDataAtTick(PredictTick, out var snapshot);

				unitTargetPosition.Value = snapshot.TargetPosition.Get(BasicUnitSnapshotData.DeQuantization);
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

				var factor = target.NearPositionCount + 1;
				
				translation.Value = math.lerp(translation.Value, target.Position, Time.deltaTime * (velocity.speed + distance + 1f));
				translation.Value = Vector3.MoveTowards(translation.Value, target.Position,  math.max(distance * 0.1f, velocity.speed * Time.deltaTime) * 0.9f * factor);
				if (target.Grounded)
				{
					translation.Value.y = 0;
				}
			});
		}
	}
}