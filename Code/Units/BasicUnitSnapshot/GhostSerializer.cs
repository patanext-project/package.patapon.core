using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.Networking.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Patapon4TLB.Core.BasicUnitSnapshot
{
	public struct BasicUnitGhostSerializer : IGhostSerializer<BasicUnitSnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<BasicUnitSnapshotData>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 100;
		}

		public bool WantsPredictionDelta => true;

		[NativeDisableContainerSafetyRestriction]
		public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

		public void BeginSerialize(ComponentSystemBase system)
		{
			GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();

			system.GetGhostComponentType(out UnitDirectionGhostType);
			system.GetGhostComponentType(out TranslationGhostType);
			system.GetGhostComponentType(out VelocityGhostType);
			system.GetGhostComponentType(out RelativePlayerGhostType);
			system.GetGhostComponentType(out RelativeTeamGhostType);
			system.GetGhostComponentType(out RelativeRhythmEngineGhostType);
		}

		public GhostComponentType<UnitDirection>                     UnitDirectionGhostType;
		public GhostComponentType<Translation>                       TranslationGhostType;
		public GhostComponentType<Velocity>                          VelocityGhostType;
		public GhostComponentType<Relative<PlayerDescription>>       RelativePlayerGhostType;
		public GhostComponentType<Relative<TeamDescription>>         RelativeTeamGhostType;
		public GhostComponentType<Relative<RhythmEngineDescription>> RelativeRhythmEngineGhostType;

		public bool CanSerialize(EntityArchetype arch)
		{
			var matches = 0;
			var comps   = arch.GetComponentTypes();
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i] == UnitDirectionGhostType) matches++;
				if (comps[i] == TranslationGhostType) matches++;
				if (comps[i] == VelocityGhostType) matches++;

				// we don't check for other components as they are not that important...
			}

			return matches == 3;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref BasicUnitSnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var unitDirection = chunk.GetNativeArray(UnitDirectionGhostType.Archetype)[ent];
			snapshot.Direction = unitDirection.Value;

			var translation = chunk.GetNativeArray(TranslationGhostType.Archetype)[ent];
			snapshot.Position.Set(BasicUnitSnapshotData.Quantization, translation.Value);

			var velocity = chunk.GetNativeArray(VelocityGhostType.Archetype)[ent];
			snapshot.Velocity.Set(BasicUnitSnapshotData.Quantization, velocity.Value);

			if (chunk.Has(RelativePlayerGhostType.Archetype))
			{
				var relativePlayer = chunk.GetNativeArray(RelativePlayerGhostType.Archetype)[ent];
				snapshot.PlayerGhostId = GetGhostId(relativePlayer.Target);
			}
			else snapshot.PlayerGhostId = 0;

			if (chunk.Has(RelativeTeamGhostType.Archetype))
			{
				var relativeTeam = chunk.GetNativeArray(RelativeTeamGhostType.Archetype)[ent];
				snapshot.TeamGhostId = GetGhostId(relativeTeam.Target);
			}
			else snapshot.TeamGhostId = 0;

			if (chunk.Has(RelativeRhythmEngineGhostType.Archetype))
			{
				var relativeRhythmEngine = chunk.GetNativeArray(RelativeRhythmEngineGhostType.Archetype)[ent];
				snapshot.RhythmEngineGhostId = GetGhostId(relativeRhythmEngine.Target);
			}
			else snapshot.RhythmEngineGhostId = 0;
		}

		private uint GetGhostId(Entity target)
		{
			if (target == default || !GhostStateFromEntity.Exists(target))
				return 0;
			return (uint) GhostStateFromEntity[target].ghostId;
		}
	}
}