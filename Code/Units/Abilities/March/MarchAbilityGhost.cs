using System;
using System.Linq;
using package.patapon.core;
using package.stormiumteam.shared;
using Runtime.Systems;
using StormiumTeam.GameBase;
using StormiumTeam.Networking.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct MarchAbilitySnapshotData : ISnapshotData<MarchAbilitySnapshotData>
	{
		public uint Tick { get; set; }

		public bool IsActive;
		public int  CommandId;
		public uint OwnerGhostId;

		public void PredictDelta(uint tick, ref MarchAbilitySnapshotData baseline1, ref MarchAbilitySnapshotData baseline2)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref MarchAbilitySnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			byte mask = 0, pos = 0;
			MainBit.SetBitAt(ref mask, pos++, IsActive);

			writer.WritePackedUInt(mask, compressionModel);
			writer.WritePackedIntDelta(CommandId, baseline.CommandId, compressionModel);
			writer.WritePackedUIntDelta(OwnerGhostId, baseline.OwnerGhostId, compressionModel);
		}

		public void Deserialize(uint tick, ref MarchAbilitySnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			byte pos  = 0;
			var  mask = (byte) reader.ReadPackedUInt(ref ctx, compressionModel);
			{
				IsActive = MainBit.GetBitAt(mask, pos++) == 1;
			}
			CommandId    = reader.ReadPackedIntDelta(ref ctx, baseline.CommandId, compressionModel);
			OwnerGhostId = reader.ReadPackedUIntDelta(ref ctx, baseline.OwnerGhostId, compressionModel);
		}

		public void Interpolate(ref MarchAbilitySnapshotData target, float factor)
		{
			IsActive     = target.IsActive;
			CommandId    = target.CommandId;
			OwnerGhostId = target.OwnerGhostId;
		}
	}

	public struct MarchAbilityGhostSerializer : IGhostSerializer<MarchAbilitySnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<MarchAbilitySnapshotData>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 10;
		}

		public bool WantsPredictionDelta => false;

		[NativeDisableContainerSafetyRestriction]
		public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

		[NativeDisableContainerSafetyRestriction]
		public ComponentDataFromEntity<RhythmCommandId> CommandIdFromEntity;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out GhostRhythmAbilityStateType);
			system.GetGhostComponentType(out GhostOwnerType);

			GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();
			CommandIdFromEntity  = system.GetComponentDataFromEntity<RhythmCommandId>();
		}

		public GhostComponentType<RhythmAbilityState> GhostRhythmAbilityStateType;
		public GhostComponentType<Owner>              GhostOwnerType;

		public bool CanSerialize(EntityArchetype arch)
		{
			var comps   = arch.GetComponentTypes();
			var matches = 0;
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i] == GhostRhythmAbilityStateType) matches++;
				if (comps[i] == GhostOwnerType) matches++;
			}

			return matches == 2;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref MarchAbilitySnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var rhythmAbilityState = chunk.GetNativeArray(GhostRhythmAbilityStateType.Archetype)[ent];
			snapshot.IsActive  = rhythmAbilityState.IsActive;
			snapshot.CommandId = rhythmAbilityState.Command == default ? 0 : CommandIdFromEntity[rhythmAbilityState.Command].Value;

			var owner = chunk.GetNativeArray(GhostOwnerType.Archetype)[ent];
			snapshot.OwnerGhostId = GetGhostId(owner.Target);
		}

		private uint GetGhostId(Entity target)
		{
			if (target == default || !GhostStateFromEntity.Exists(target))
				return 0;
			return (uint) GhostStateFromEntity[target].ghostId;
		}
	}

	public class MarchAbilityGhostSpawnSystem : DefaultGhostSpawnSystem<MarchAbilitySnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			World.GetOrCreateSystem<MarchAbilityProvider>().GetComponents(out var baseArchetype);

			return EntityManager.CreateArchetype(baseArchetype.Union(new ComponentType[]
			{
				typeof(MarchAbilitySnapshotData),
				typeof(GhostOwner),

				typeof(ReplicatedEntityComponent)
			}).ToArray());
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return GetGhostArchetype();
		}
	}

	[UpdateInGroup(typeof(UpdateGhostSystemGroup))]
	public class MarchAbilityGhostUpdateSystem : JobComponentSystem
	{
		private struct Job : IJobForEachWithEntity<RhythmAbilityState, MarchAbility, GhostOwner>
		{
			[ReadOnly] public uint                                       TargetTick;
			[ReadOnly] public BufferFromEntity<MarchAbilitySnapshotData> SnapshotDataFromEntity;
			[ReadOnly] public NativeHashMap<int, Entity>                 CommandIdToEntity;

			public void Execute(Entity entity, int index, ref RhythmAbilityState state, ref MarchAbility marchAbility, ref GhostOwner owner)
			{
				SnapshotDataFromEntity[entity].GetDataAtTick(TargetTick, out var snapshot);

				state.IsActive = snapshot.IsActive;
				state.Command  = snapshot.CommandId == 0 ? default : CommandIdToEntity[snapshot.CommandId];
				owner.GhostId  = (int) snapshot.OwnerGhostId;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				TargetTick             = NetworkTimeSystem.interpolateTargetTick,
				SnapshotDataFromEntity = GetBufferFromEntity<MarchAbilitySnapshotData>(),

				CommandIdToEntity = World.GetExistingSystem<RhythmCommandManager>().CommandIdToEntity
			}.Schedule(this, inputDeps);
		}
	}
}