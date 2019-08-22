using System;
using System.Linq;
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

namespace Patapon4TLB.Default
{
	public struct RetreatAbilitySnapshotData : ISnapshotData<RetreatAbilitySnapshotData>
	{
		public uint Tick { get; set; }

		public bool IsActive;
		public bool IsStillChaining;
		public bool ClientPredictState;

		public int  CommandId;
		public uint OwnerGhostId;

		public void PredictDelta(uint tick, ref RetreatAbilitySnapshotData baseline1, ref RetreatAbilitySnapshotData baseline2)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref RetreatAbilitySnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			byte mask = 0, pos = 0;
			MainBit.SetBitAt(ref mask, pos++, IsActive);
			MainBit.SetBitAt(ref mask, pos++, IsStillChaining);
			MainBit.SetBitAt(ref mask, pos++, ClientPredictState);

			writer.WritePackedUInt(mask, compressionModel);
			writer.WritePackedIntDelta(CommandId, baseline.CommandId, compressionModel);
			writer.WritePackedUIntDelta(OwnerGhostId, baseline.OwnerGhostId, compressionModel);
		}

		public void Deserialize(uint tick, ref RetreatAbilitySnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			byte pos  = 0;
			var  mask = (byte) reader.ReadPackedUInt(ref ctx, compressionModel);
			{
				IsActive           = MainBit.GetBitAt(mask, pos++) == 1;
				IsStillChaining    = MainBit.GetBitAt(mask, pos++) == 1;
				ClientPredictState = MainBit.GetBitAt(mask, pos++) == 1;
			}
			CommandId    = reader.ReadPackedIntDelta(ref ctx, baseline.CommandId, compressionModel);
			OwnerGhostId = reader.ReadPackedUIntDelta(ref ctx, baseline.OwnerGhostId, compressionModel);
		}

		public void Interpolate(ref RetreatAbilitySnapshotData target, float factor)
		{
			IsActive           = target.IsActive;
			IsStillChaining    = target.IsStillChaining;
			ClientPredictState = target.ClientPredictState;

			CommandId    = target.CommandId;
			OwnerGhostId = target.OwnerGhostId;
		}
	}

	public struct RetreatAbilityGhostSerializer : IGhostSerializer<RetreatAbilitySnapshotData>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<RetreatAbilitySnapshotData>();

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
			system.GetGhostComponentType(out GhostRetreatAbilityType);
			system.GetGhostComponentType(out GhostOwnerType);

			GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();
			CommandIdFromEntity  = system.GetComponentDataFromEntity<RhythmCommandId>();
		}

		public GhostComponentType<RhythmAbilityState> GhostRhythmAbilityStateType;
		public GhostComponentType<RetreatAbility>     GhostRetreatAbilityType;
		public GhostComponentType<Owner>              GhostOwnerType;

		public bool CanSerialize(EntityArchetype arch)
		{
			var comps   = arch.GetComponentTypes();
			var matches = 0;
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i] == GhostRhythmAbilityStateType) matches++;
				if (comps[i] == GhostRetreatAbilityType) matches++;
				if (comps[i] == GhostOwnerType) matches++;
			}

			return matches == 3;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref RetreatAbilitySnapshotData snapshot)
		{
			snapshot.Tick = tick;

			snapshot.ClientPredictState = true; // how should we manage that? it should be the default value, right?

			var rhythmAbilityState = chunk.GetNativeArray(GhostRhythmAbilityStateType.Archetype)[ent];
			snapshot.IsActive        = rhythmAbilityState.IsActive;
			snapshot.IsStillChaining = rhythmAbilityState.IsStillChaining;
			snapshot.CommandId       = rhythmAbilityState.Command == default ? 0 : CommandIdFromEntity[rhythmAbilityState.Command].Value;

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

	public class RetreatAbilityGhostSpawnSystem : DefaultGhostSpawnSystem<RetreatAbilitySnapshotData>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			World.GetOrCreateSystem<RetreatAbilityProvider>().GetComponents(out var baseArchetype);

			return EntityManager.CreateArchetype(baseArchetype.Union(new ComponentType[]
			{
				typeof(RetreatAbilitySnapshotData),

				typeof(ReplicatedEntityComponent)
			}).ToArray());
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			return GetGhostArchetype();
		}
	}

	[UpdateInGroup(typeof(UpdateGhostSystemGroup))]
	public class RetreatAbilityGhostUpdateSystem : JobComponentSystem
	{
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<RhythmAbilityState, RetreatAbility, Owner>
		{
			[ReadOnly] public UTick                                        ServerTick;
			[ReadOnly] public BufferFromEntity<RetreatAbilitySnapshotData> SnapshotDataFromEntity;
			[ReadOnly] public NativeHashMap<int, Entity>                   CommandIdToEntity;
			[ReadOnly] public NativeHashMap<int, Entity>                   GhostEntityMap;

			public RhythmEngineDataGroup RhythmEngineDataGroup;

			[ReadOnly] public ComponentDataFromEntity<Relative<RhythmEngineDescription>> RelativeRhythmEngineFromEntity;

			public void Execute(Entity entity, int index, ref RhythmAbilityState state, ref RetreatAbility RetreatAbility, ref Owner owner)
			{
				SnapshotDataFromEntity[entity].GetDataAtTick(ServerTick.AsUInt, out var snapshot);

				GhostEntityMap.TryGetValue((int) snapshot.OwnerGhostId, out owner.Target);
				CommandIdToEntity.TryGetValue(snapshot.CommandId, out state.Command);

				RhythmEngineDataGroup.Result result = default;
				var predict = snapshot.ClientPredictState && RelativeRhythmEngineFromEntity.Exists(owner.Target);
				if (predict)
				{
					var rhythmEngine = RelativeRhythmEngineFromEntity[owner.Target].Target;
					if (rhythmEngine == default)
						predict = false;
					else
					{
						result = RhythmEngineDataGroup.GetResult(rhythmEngine);

						state.Calculate(result.CurrentCommand, result.CommandState, result.ComboState, result.EngineProcess);
					}
				}

				if (predict)
				{
					if (state.IsActive || state.IsStillChaining)
					{
						RetreatAbility.ActiveTime   = (result.EngineProcess.Milliseconds - state.StartTime) * 0.001f;
						RetreatAbility.IsRetreating = RetreatAbility.ActiveTime <= 2.0f;
					}
					else
					{
						RetreatAbility.ActiveTime   = 0.0f;
						RetreatAbility.IsRetreating = false;
					}
				}

				if (!predict)
				{
					state.IsActive        = snapshot.IsActive;
					state.IsStillChaining = snapshot.IsStillChaining;
				}

				state.IsActive = snapshot.IsActive;
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
				ServerTick             = m_NetworkTimeSystem.GetTickInterpolated(),
				SnapshotDataFromEntity = GetBufferFromEntity<RetreatAbilitySnapshotData>(),

				CommandIdToEntity = m_CommandManager.CommandIdToEntity,
				GhostEntityMap    = m_ConvertGhostEntityMap.HashMap,

				RhythmEngineDataGroup          = new RhythmEngineDataGroup(this),
				RelativeRhythmEngineFromEntity = GetComponentDataFromEntity<Relative<RhythmEngineDescription>>(true)
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, m_ConvertGhostEntityMap.dependency));

			return inputDeps;
		}
	}
}