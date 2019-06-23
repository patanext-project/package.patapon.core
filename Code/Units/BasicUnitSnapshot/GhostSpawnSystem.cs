using package.StormiumTeam.GameBase;
using Patapon4TLB.Default;
using Runtime.Systems;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Patapon4TLB.Core.BasicUnitSnapshot
{
	public class BasicUnitGhostSpawnSystem : DefaultGhostSpawnSystem<BasicUnitSnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(BasicUnitSnapshotData),
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
	[UpdateAfter(typeof(BasicUnitGhostSpawnSystem))]
	[UpdateBefore(typeof(ConvertGhostToOwnerSystem))]
	[UpdateBefore(typeof(ConvertGhostToRelativeSystemGroup))]
	public class BasicUnitUpdateSystem : JobComponentSystem
	{
		private struct Job : IJobForEachWithEntity<UnitDirection, Translation, Velocity>
		{
			public uint Tick;

			[ReadOnly] public BufferFromEntity<BasicUnitSnapshotData> SnapshotDataFromEntity;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GhostRelative<PlayerDescription>>       RelativePlayerFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GhostRelative<TeamDescription>>         RelativeTeamFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GhostRelative<RhythmEngineDescription>> RelativeRhythmEngineFromEntity;

			public void Execute(Entity entity, int jobIndex, ref UnitDirection unitDirection, ref Translation translation, ref Velocity velocity)
			{
				SnapshotDataFromEntity[entity].GetDataAtTick(Tick, out var snapshot);

				unitDirection.Value = (sbyte) snapshot.Direction;
				translation.Value   = snapshot.Position.Get(BasicUnitSnapshotData.DeQuantization);
				velocity.Value      = snapshot.Velocity.Get(BasicUnitSnapshotData.DeQuantization);

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
				Tick = NetworkTimeSystem.interpolateTargetTick,

				SnapshotDataFromEntity = GetBufferFromEntity<BasicUnitSnapshotData>(true),

				RelativePlayerFromEntity       = GetComponentDataFromEntity<GhostRelative<PlayerDescription>>(),
				RelativeTeamFromEntity         = GetComponentDataFromEntity<GhostRelative<TeamDescription>>(),
				RelativeRhythmEngineFromEntity = GetComponentDataFromEntity<GhostRelative<RhythmEngineDescription>>(),
			}.Schedule(this, inputDeps);
		}
	}
}