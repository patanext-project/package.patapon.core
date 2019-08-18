using System;
using System.Linq;
using DefaultNamespace;
using package.patapon.core;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using StormiumTeam.Networking.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon4TLB.Default.Attack
{
	public struct BasicTaterazayAttackAbilitySnapshotData : ISnapshotData<BasicTaterazayAttackAbilitySnapshotData>
	{
		public uint Tick { get; set; }

		public bool ClientPredictState;
		public bool IsActive;
		public int  CommandId;
		public uint  StartAttackTick;
		public uint OwnerGhostId;

		public void PredictDelta(uint tick, ref BasicTaterazayAttackAbilitySnapshotData baseline1, ref BasicTaterazayAttackAbilitySnapshotData baseline2)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref BasicTaterazayAttackAbilitySnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			byte mask = 0;
			{
				MainBit.SetBitAt(ref mask, 0, ClientPredictState);
				MainBit.SetBitAt(ref mask, 1, IsActive);
			}

			writer.WritePackedUInt(mask, compressionModel);
			writer.WritePackedIntDelta(CommandId, baseline.CommandId, compressionModel);
			writer.WritePackedUIntDelta(StartAttackTick, baseline.StartAttackTick, compressionModel);
			writer.WritePackedUIntDelta(OwnerGhostId, baseline.OwnerGhostId, compressionModel);
		}

		public void Deserialize(uint tick, ref BasicTaterazayAttackAbilitySnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			var mask = reader.ReadPackedUInt(ref ctx, compressionModel);
			{
				ClientPredictState = MainBit.GetBitAt(mask, 0) == 1;
				IsActive           = MainBit.GetBitAt(mask, 1) == 1;
			}

			CommandId       = reader.ReadPackedIntDelta(ref ctx, baseline.CommandId, compressionModel);
			StartAttackTick = reader.ReadPackedUIntDelta(ref ctx, baseline.StartAttackTick, compressionModel);
			OwnerGhostId    = reader.ReadPackedUIntDelta(ref ctx, baseline.OwnerGhostId, compressionModel);
		}

		public void Interpolate(ref BasicTaterazayAttackAbilitySnapshotData target, float factor)
		{
			Tick               = target.Tick;
			ClientPredictState = target.ClientPredictState;
			IsActive           = target.IsActive;
			StartAttackTick    = target.StartAttackTick;
		}
	}

	public struct BasicTaterazayAttackAbilityGhostSerializer : IGhostSerializer<BasicTaterazayAttackAbilitySnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<BasicTaterazayAttackAbilitySnapshotData>();

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
			GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();
			CommandIdFromEntity  = system.GetComponentDataFromEntity<RhythmCommandId>();

			system.GetGhostComponentType(out GhostRhythmAbilityStateType);
			system.GetGhostComponentType(out GhostAttackAbilityType);
			system.GetGhostComponentType(out GhostOwnerType);
		}

		public GhostComponentType<RhythmAbilityState>          GhostRhythmAbilityStateType;
		public GhostComponentType<BasicTaterazayAttackAbility> GhostAttackAbilityType;
		public GhostComponentType<Owner>                       GhostOwnerType;

		public bool CanSerialize(EntityArchetype arch)
		{
			var comps = arch.GetComponentTypes();
			var count = 0;
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i] == GhostRhythmAbilityStateType.ComponentType) count++;
				if (comps[i] == GhostAttackAbilityType.ComponentType) count++;
				if (comps[i] == GhostOwnerType.ComponentType) count++;
			}

			return count == 3;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref BasicTaterazayAttackAbilitySnapshotData snapshot)
		{
			snapshot.Tick = tick;

			var abilityState  = chunk.GetNativeArray(GhostRhythmAbilityStateType.Archetype)[ent];
			var attackAbility = chunk.GetNativeArray(GhostAttackAbilityType.Archetype)[ent];
			var owner         = chunk.GetNativeArray(GhostOwnerType.Archetype)[ent];

			snapshot.IsActive           = abilityState.IsActive;
			snapshot.CommandId          = abilityState.Command == default ? 0 : CommandIdFromEntity[abilityState.Command].Value;
			snapshot.StartAttackTick    = attackAbility.AttackStartTick;
			snapshot.OwnerGhostId       = GhostStateFromEntity.GetGhostId(owner.Target);
			snapshot.ClientPredictState = false;
		}
	}

	public class BasicTaterazayAttackAbilitySpawn : DefaultGhostSpawnSystem<BasicTaterazayAttackAbilitySnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			World.GetOrCreateSystem<BasicTaterazayAttackAbility.Provider>().GetComponents(out var baseArchetype);

			return EntityManager.CreateArchetype(baseArchetype.Union(new ComponentType[]
			{
				typeof(BasicTaterazayAttackAbilitySnapshotData),
				typeof(ReplicatedEntityComponent)
			}).ToArray());
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return GetGhostArchetype();
		}
	}

	[UpdateInGroup(typeof(UpdateGhostSystemGroup))]
	public class BasicTaterazayAttackAbilityUpdateSystem : JobComponentSystem
	{
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<RhythmAbilityState, BasicTaterazayAttackAbility, Owner>
		{
			[ReadOnly] public UTick                                                     TargetTick;
			[ReadOnly] public BufferFromEntity<BasicTaterazayAttackAbilitySnapshotData> SnapshotDataFromEntity;
			[ReadOnly] public NativeHashMap<int, Entity>                                CommandIdToEntity;
			[ReadOnly] public NativeHashMap<int, Entity>                                GhostEntityMap;

			public RhythmEngineDataGroup RhythmEngineDataGroup;

			[ReadOnly] public ComponentDataFromEntity<Relative<RhythmEngineDescription>> RelativeRhythmEngineFromEntity;

			public void Execute(Entity entity, int index, ref RhythmAbilityState state, ref BasicTaterazayAttackAbility attackAbility, ref Owner owner)
			{
				var buffer = SnapshotDataFromEntity[entity];
				if (buffer.Length == 0)
					return;

				var snapshot = buffer[buffer.Length - 1];

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
					var startAttackTick = UTick.CopyDelta(TargetTick, snapshot.StartAttackTick);

					state.IsActive                = snapshot.IsActive;
					attackAbility.AttackStartTick = snapshot.StartAttackTick;
					attackAbility.HasSlashed      = UTick.AddMs(startAttackTick, BasicTaterazayAttackAbility.DelaySlashMs) <= TargetTick;
				}

				state.IsActive = snapshot.IsActive;
			}
		}

		private ConvertGhostEntityMap            m_ConvertGhostEntityMap;
		private RhythmCommandManager             m_CommandManager;
		private NetworkTimeSystem                m_NetworkTimeSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ConvertGhostEntityMap            = World.GetOrCreateSystem<ConvertGhostEntityMap>();
			m_CommandManager                   = World.GetOrCreateSystem<RhythmCommandManager>();
			m_NetworkTimeSystem                = World.GetOrCreateSystem<NetworkTimeSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				TargetTick             = m_NetworkTimeSystem.GetTickPredicted(),
				SnapshotDataFromEntity = GetBufferFromEntity<BasicTaterazayAttackAbilitySnapshotData>(),

				CommandIdToEntity = m_CommandManager.CommandIdToEntity,
				GhostEntityMap    = m_ConvertGhostEntityMap.HashMap,

				RhythmEngineDataGroup          = new RhythmEngineDataGroup(this),
				RelativeRhythmEngineFromEntity = GetComponentDataFromEntity<Relative<RhythmEngineDescription>>(true)
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, m_ConvertGhostEntityMap.dependency));

			return inputDeps;
		}
	}
}