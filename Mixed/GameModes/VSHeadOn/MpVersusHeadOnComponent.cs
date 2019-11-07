using Revolution;
using StormiumTeam.GameBase;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	public struct VersusHeadOnUnit : IComponentData
	{
		public int Team;
		public int FormationIndex;

		public int KillStreak;
		public int DeadCount;

		public UTick TickBeforeSpawn;
	}

	public struct VersusHeadOnPlayer : IComponentData
	{

	}

	public struct MpVersusHeadOn : IGameMode, IReadWriteComponentSnapshot<MpVersusHeadOn, GhostSetup>
	{
		public struct Exclude : IComponentData
		{}
		
		public class Synchronizer : MixedComponentSnapshotSystem<MpVersusHeadOn, GhostSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
		
		public enum State
		{
			OnInitialization,
			OnLoadingMap,
			OnMapStart,
			OnRoundStart,
			Playing,
			OnRoundEnd,
			OnMapEnd,
		}

		public State PlayState;
		public int   EndTime;

		public Entity Team0, Team1;

		// we can't use fixed buffer in burst???
		public int Team0Points;
		public int Team1Points;
		public int Team0Eliminations;
		public int Team1Eliminations;

		public int GetPointReadOnly(int team)
		{
			if (team == 0) return Team0Points;
			return Team1Points;
		}

		public int GetEliminationReadOnly(int team)
		{
			if (team == 0) return Team0Eliminations;
			return Team1Eliminations;
		}

		public unsafe ref int GetPoints(int team)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			Debug.Assert(team >= 0 && team <= 1, "team >= 0 && team <= 1");
#endif
			return ref UnsafeUtilityEx.ArrayElementAsRef<int>(UnsafeUtility.AddressOf(ref Team0Points), team);
		}

		public unsafe ref int GetEliminations(int team)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			Debug.Assert(team >= 0 && team <= 1, "team >= 0 && team <= 1");
#endif
			return ref UnsafeUtilityEx.ArrayElementAsRef<int>(UnsafeUtility.AddressOf(ref Team0Eliminations), team);
		}

		public uint Tick { get; set; }

		public void WriteTo(DataStreamWriter writer, ref MpVersusHeadOn baseline, GhostSetup setup, SerializeClientData jobData)
		{
			var compression = jobData.NetworkCompressionModel;

			writer.WritePackedUIntDelta((uint) PlayState, (uint) baseline.PlayState, compression);
			writer.WritePackedIntDelta(EndTime, baseline.EndTime, compression);

			writer.WritePackedUInt(setup[Team0], jobData.NetworkCompressionModel);
			writer.WritePackedUInt(setup[Team1], jobData.NetworkCompressionModel);

			for (var i = 0; i != 2; i++)
			{
				writer.WritePackedIntDelta(GetPointReadOnly(i), baseline.GetPointReadOnly(i), compression);
				writer.WritePackedIntDelta(GetEliminationReadOnly(i), baseline.GetEliminationReadOnly(i), compression);
			}
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref MpVersusHeadOn baseline, DeserializeClientData jobData)
		{
			var compression = jobData.NetworkCompressionModel;

			PlayState = (MpVersusHeadOn.State) reader.ReadPackedUIntDelta(ref ctx, (uint) baseline.PlayState, compression);
			EndTime   = reader.ReadPackedIntDelta(ref ctx, baseline.EndTime, compression);

			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, compression), out Team0);
			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, compression), out Team1);

			for (var i = 0; i != 2; i++)
			{
				GetPoints(i)       = reader.ReadPackedIntDelta(ref ctx, baseline.GetPointReadOnly(i), compression);
				GetEliminations(i) = reader.ReadPackedIntDelta(ref ctx, baseline.GetEliminationReadOnly(i), compression);
			}
		}
	}
}