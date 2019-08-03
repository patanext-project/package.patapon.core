using DefaultNamespace;
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
			system.GetGhostComponentType(out UnitTargetPositionGhostType);
			system.GetGhostComponentType(out TranslationGhostType);
			system.GetGhostComponentType(out VelocityGhostType);
		}

		public GhostComponentType<UnitDirection>                     UnitDirectionGhostType;
		public GhostComponentType<UnitTargetPosition>                UnitTargetPositionGhostType;
		public GhostComponentType<Translation>                       TranslationGhostType;
		public GhostComponentType<Velocity>                          VelocityGhostType;

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

			var targetPosition = chunk.GetNativeArray(UnitTargetPositionGhostType.Archetype)[ent];
			snapshot.TargetPosition.Set(BasicUnitSnapshotData.Quantization, targetPosition.Value);

			var translation = chunk.GetNativeArray(TranslationGhostType.Archetype)[ent];
			snapshot.Position.Set(BasicUnitSnapshotData.Quantization, translation.Value);

			var velocity = chunk.GetNativeArray(VelocityGhostType.Archetype)[ent];
			snapshot.Velocity.Set(BasicUnitSnapshotData.Quantization, velocity.Value);

			snapshot.GroundFlags = translation.Value.y <= 0.0001f ? (byte) 1 : (byte) 0;
		}
	}
}