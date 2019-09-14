using System;
using System.Linq;
using package.patapon.core;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Revolution.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct MarchAbilitySnapshotData : ISnapshotData<MarchAbilitySnapshotData>
	{
		public uint Tick { get; set; }

		public bool IsActive;
		public bool ClientPredictState;

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
			MainBit.SetBitAt(ref mask, pos++, ClientPredictState);

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
				IsActive           = MainBit.GetBitAt(mask, pos++) == 1;
				ClientPredictState = MainBit.GetBitAt(mask, pos++) == 1;
			}
			CommandId    = reader.ReadPackedIntDelta(ref ctx, baseline.CommandId, compressionModel);
			OwnerGhostId = reader.ReadPackedUIntDelta(ref ctx, baseline.OwnerGhostId, compressionModel);
		}

		public void Interpolate(ref MarchAbilitySnapshotData target, float factor)
		{
			IsActive           = target.IsActive;
			ClientPredictState = target.ClientPredictState;

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
			system.GetGhostComponentType(out GhostMarchAbilityType);
			system.GetGhostComponentType(out GhostOwnerType);

			GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();
			CommandIdFromEntity  = system.GetComponentDataFromEntity<RhythmCommandId>();
		}

		public GhostComponentType<RhythmAbilityState> GhostRhythmAbilityStateType;
		public GhostComponentType<MarchAbility>       GhostMarchAbilityType;
		public GhostComponentType<Owner>              GhostOwnerType;

		public bool CanSerialize(EntityArchetype arch)
		{
			var comps   = arch.GetComponentTypes();
			var matches = 0;
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i] == GhostRhythmAbilityStateType) matches++;
				if (comps[i] == GhostMarchAbilityType) matches++;
				if (comps[i] == GhostOwnerType) matches++;
			}

			return matches == 3;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref MarchAbilitySnapshotData snapshot)
		{
			snapshot.Tick = tick;

			snapshot.ClientPredictState = true; // how should we manage that? it should be the default value, right?

			var rhythmAbilityState = chunk.GetNativeArray(GhostRhythmAbilityStateType.Archetype)[ent];
			snapshot.IsActive  = rhythmAbilityState.IsActive;
			snapshot.CommandId = rhythmAbilityState.Command == default ? 0 : CommandIdFromEntity[rhythmAbilityState.Command].Value;

			var owner = chunk.GetNativeArray(GhostOwnerType.Archetype)[ent];
			snapshot.OwnerGhostId = GhostStateFromEntity.GetGhostId(owner.Target);
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
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<RhythmAbilityState, MarchAbility, Owner>
		{
			[ReadOnly] public UTick                                      ServerTick;
			[ReadOnly] public BufferFromEntity<MarchAbilitySnapshotData> SnapshotDataFromEntity;
			[ReadOnly] public NativeHashMap<int, Entity>                 CommandIdToEntity;

			[ReadOnly]
			public NativeHashMap<int, Entity> GhostEntityMap;

			public RhythmEngineDataGroup RhythmEngineDataGroup;

			[ReadOnly] public ComponentDataFromEntity<Relative<RhythmEngineDescription>> RelativeRhythmEngineFromEntity;

			public void Execute(Entity entity, int index, ref RhythmAbilityState state, ref MarchAbility marchAbility, ref Owner owner)
			{
				SnapshotDataFromEntity[entity].GetDataAtTick(ServerTick.AsUInt, out var snapshot);

				GhostEntityMap.TryGetValue((int) snapshot.OwnerGhostId, out owner.Target);
				CommandIdToEntity.TryGetValue(snapshot.CommandId, out state.Command);

				var predict = snapshot.ClientPredictState && RelativeRhythmEngineFromEntity.Exists(owner.Target);
				if (predict)
				{
					var rhythmEngine = RelativeRhythmEngineFromEntity[owner.Target].Target;
					if (rhythmEngine == default)
						predict = false;
					else
					{
						var result = RhythmEngineDataGroup.GetResult(rhythmEngine);

						state.Calculate(result.CurrentCommand, result.CommandState, result.ComboState, result.EngineProcess);
					}
				}

				if (!predict)
				{
					state.IsActive = snapshot.IsActive;
				}
			}
		}

		private ConvertGhostEntityMap m_ConvertGhostEntityMap;
		private RhythmCommandManager  m_CommandManager;
		private NetworkTimeSystem     m_NetworkTimeSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ConvertGhostEntityMap = World.GetOrCreateSystem<ConvertGhostEntityMap>();
			m_CommandManager        = World.GetOrCreateSystem<RhythmCommandManager>();
			m_NetworkTimeSystem     = World.GetOrCreateSystem<NetworkTimeSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				ServerTick = m_NetworkTimeSystem.GetTickInterpolated(),

				SnapshotDataFromEntity = GetBufferFromEntity<MarchAbilitySnapshotData>(true),
				CommandIdToEntity      = m_CommandManager.CommandIdToEntity,

				RelativeRhythmEngineFromEntity = GetComponentDataFromEntity<Relative<RhythmEngineDescription>>(true),
				RhythmEngineDataGroup          = new RhythmEngineDataGroup(this),

				GhostEntityMap = m_ConvertGhostEntityMap.HashMap
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, m_ConvertGhostEntityMap.dependency));

			return inputDeps;
		}
	}
}