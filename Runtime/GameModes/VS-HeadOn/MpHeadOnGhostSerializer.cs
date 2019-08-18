using System;
using DefaultNamespace;
using StormiumTeam.Networking.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	public class MpHeadOnGhostSerializer
	{
		public struct MpHeadOnGameModeSerializer : IGhostSerializer<MpHeadOnGameModeSnapshot>
		{
			public int SnapshotSize => UnsafeUtility.SizeOf<MpHeadOnGameModeSnapshot>();

			public int CalculateImportance(ArchetypeChunk chunk)
			{
				return 1;
			}

			public bool WantsPredictionDelta => false;

			public GhostComponentType<MpVersusHeadOn> GameModeDataType;

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

			public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref MpHeadOnGameModeSnapshot snapshot)
			{
				var data = chunk.GetNativeArray(GameModeDataType.Archetype)[ent];
				snapshot.Tick      = tick;
				snapshot.PlayState = data.PlayState;
				snapshot.EndTime   = data.EndTime;

				snapshot.Team0GhostId = GhostStateFromEntity.GetGhostId(data.Team0);
				snapshot.Team1GhostId = GhostStateFromEntity.GetGhostId(data.Team1);

				snapshot.Team0Score       = data.GetPointReadOnly(0);
				snapshot.Team1Score       = data.GetPointReadOnly(1);
				snapshot.Team0Elimination = data.GetEliminationReadOnly(0);
				snapshot.Team1Elimination = data.GetEliminationReadOnly(1);
			}
		}

		public struct MpHeadOnGameModeSnapshot : ISnapshotData<MpHeadOnGameModeSnapshot>
		{
			public uint Tick { get; set; }

			public MpVersusHeadOn.State PlayState;
			public int                  EndTime;

			public uint Team0GhostId, Team1GhostId;

			public int Team0Score;
			public int Team1Score;

			public int Team0Elimination;
			public int Team1Elimination;

			public void PredictDelta(uint tick, ref MpHeadOnGameModeSnapshot baseline1, ref MpHeadOnGameModeSnapshot baseline2)
			{
				throw new NotImplementedException();
			}

			public void Serialize(ref MpHeadOnGameModeSnapshot baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedUIntDelta((uint) PlayState, (uint) baseline.PlayState, compressionModel);
				writer.WritePackedIntDelta(EndTime, baseline.EndTime, compressionModel);

				writer.WritePackedUIntDelta(Team0GhostId, baseline.Team0GhostId, compressionModel);
				writer.WritePackedUIntDelta(Team1GhostId, baseline.Team1GhostId, compressionModel);

				writer.WritePackedIntDelta(Team0Score, baseline.Team0Score, compressionModel);
				writer.WritePackedIntDelta(Team1Score, baseline.Team1Score, compressionModel);
				writer.WritePackedIntDelta(Team0Elimination, baseline.Team0Elimination, compressionModel);
				writer.WritePackedIntDelta(Team1Elimination, baseline.Team1Elimination, compressionModel);
			}

			public void Deserialize(uint tick, ref MpHeadOnGameModeSnapshot baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
			{
				Tick = tick;

				PlayState = (MpVersusHeadOn.State) reader.ReadPackedUIntDelta(ref ctx, (uint) baseline.PlayState, compressionModel);
				EndTime   = reader.ReadPackedIntDelta(ref ctx, baseline.EndTime, compressionModel);

				Team0GhostId       = reader.ReadPackedUIntDelta(ref ctx, baseline.Team0GhostId, compressionModel);
				Team1GhostId       = reader.ReadPackedUIntDelta(ref ctx, baseline.Team1GhostId, compressionModel);
				
				Team0Score       = reader.ReadPackedIntDelta(ref ctx, baseline.Team0Score, compressionModel);
				Team1Score       = reader.ReadPackedIntDelta(ref ctx, baseline.Team1Score, compressionModel);
				Team0Elimination = reader.ReadPackedIntDelta(ref ctx, baseline.Team0Elimination, compressionModel);
				Team1Elimination = reader.ReadPackedIntDelta(ref ctx, baseline.Team1Elimination, compressionModel);
			}

			public void Interpolate(ref MpHeadOnGameModeSnapshot target, float factor)
			{
				this = target;
			}
		}

		public class MpHeadOnGameModeSnapshotGhostSpawnSystem : DefaultGhostSpawnSystem<MpHeadOnGameModeSnapshot>
		{
			protected override EntityArchetype GetGhostArchetype()
			{
				return EntityManager.CreateArchetype
				(
					typeof(MpVersusHeadOn),
					typeof(MpHeadOnGameModeSnapshot),
					typeof(ReplicatedEntityComponent)
				);
			}

			protected override EntityArchetype GetPredictedGhostArchetype()
			{
				// will never be used...
				return GetGhostArchetype();
			}
		}

		public class MpHeadOnGameModeUpdateFromSnapshotSystem : BaseUpdateFromSnapshotSystem<MpHeadOnGameModeSnapshot, MpVersusHeadOn>
		{
		}
	}
}