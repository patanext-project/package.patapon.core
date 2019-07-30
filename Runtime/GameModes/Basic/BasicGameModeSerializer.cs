using System;
using DefaultNamespace;
using StormiumTeam.Networking.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.GameModes.Basic
{
	public struct BasicGameModeSerializer : IGhostSerializer<BasicGameModeSnapshot>
	{
		public int SnapshotSize => UnsafeUtility.SizeOf<BasicGameModeSnapshot>();

		public int CalculateImportance(ArchetypeChunk chunk)
		{
			return 1;
		}

		public bool WantsPredictionDelta => false;

		public GhostComponentType<BasicGameModeData> GameModeDataType;

		[NativeDisableContainerSafetyRestriction]
		public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

		public void BeginSerialize(ComponentSystemBase system)
		{
			system.GetGhostComponentType(out GameModeDataType);

			GhostStateFromEntity = system.GetComponentDataFromEntity<GhostSystemStateComponent>();
		}

		public bool CanSerialize(EntityArchetype arch)
		{
			var matches = 0;
			var comps   = arch.GetComponentTypes();
			for (var i = 0; i != comps.Length; i++)
			{
				if (comps[i] == GameModeDataType) matches++;
			}

			return matches == 1;
		}

		public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref BasicGameModeSnapshot snapshot)
		{
			var data = chunk.GetNativeArray(GameModeDataType.Archetype)[ent];
			snapshot.Tick              = tick;
			snapshot.StartTime         = data.StartTime;
			snapshot.PlayerTeamGhostId = GhostStateFromEntity.GetGhostId(data.PlayerTeam);
			snapshot.EnemyTeamGhostId  = GhostStateFromEntity.GetGhostId(data.EnemyTeam);
		}
	}

	public struct BasicGameModeSnapshot : ISnapshotData<BasicGameModeSnapshot>
	{
		public uint Tick { get; set; }

		public uint StartTime;
		public uint PlayerTeamGhostId;
		public uint EnemyTeamGhostId;

		public void PredictDelta(uint tick, ref BasicGameModeSnapshot baseline1, ref BasicGameModeSnapshot baseline2)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref BasicGameModeSnapshot baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedUIntDelta(StartTime, baseline.StartTime, compressionModel);
			writer.WritePackedUIntDelta(PlayerTeamGhostId, baseline.PlayerTeamGhostId, compressionModel);
			writer.WritePackedUIntDelta(EnemyTeamGhostId, baseline.EnemyTeamGhostId, compressionModel);
		}

		public void Deserialize(uint tick, ref BasicGameModeSnapshot baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			StartTime         = reader.ReadPackedUIntDelta(ref ctx, baseline.StartTime, compressionModel);
			PlayerTeamGhostId = reader.ReadPackedUIntDelta(ref ctx, baseline.PlayerTeamGhostId, compressionModel);
			EnemyTeamGhostId  = reader.ReadPackedUIntDelta(ref ctx, baseline.EnemyTeamGhostId, compressionModel);
		}

		public void Interpolate(ref BasicGameModeSnapshot target, float factor)
		{
			StartTime         = target.StartTime;
			PlayerTeamGhostId = target.PlayerTeamGhostId;
			EnemyTeamGhostId = target.EnemyTeamGhostId;
		}
	}

	public class BasicGameModeSnapshotGhostSpawnSystem : DefaultGhostSpawnSystem<BasicGameModeSnapshot>
	{
		protected override EntityArchetype GetGhostArchetype()
		{
			return EntityManager.CreateArchetype
			(
				typeof(BasicGameModeData),
				typeof(BasicGameModeSnapshot),
				typeof(ReplicatedEntityComponent)
			);
		}

		protected override EntityArchetype GetPredictedGhostArchetype()
		{
			// will never be used...
			return GetGhostArchetype();
		}
	}

	public class BasicGameModeUpdateFromSnapshotSystem : BaseUpdateFromSnapshotSystem<BasicGameModeSnapshot, BasicGameModeData>
	{
	}
}