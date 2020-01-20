using System.Runtime.CompilerServices;
using Revolution;
using Scripts.Utilities;
using StormiumTeam.GameBase;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;
using Utilities;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	public struct VersusHeadOnUnit : IReadWriteComponentSnapshot<VersusHeadOnUnit>, ISnapshotDelta<VersusHeadOnUnit>
	{
		public int Team;
		public int FormationIndex;

		public int KillStreak;
		public int DeadCount;

		public long TickBeforeSpawn;

		public void WriteTo(DataStreamWriter writer, ref VersusHeadOnUnit baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedInt(Team, jobData.NetworkCompressionModel);
			writer.WritePackedInt(FormationIndex, jobData.NetworkCompressionModel);
			writer.WritePackedInt(KillStreak, jobData.NetworkCompressionModel);
			writer.WritePackedInt(DeadCount, jobData.NetworkCompressionModel);
			writer.WritePackedLong(TickBeforeSpawn, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref VersusHeadOnUnit baseline, DeserializeClientData jobData)
		{
			Team            = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			FormationIndex  = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			KillStreak      = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			DeadCount       = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			TickBeforeSpawn = reader.ReadPackedLong(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(VersusHeadOnUnit baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<VersusHeadOnUnit>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	public struct VersusHeadOnPlayer : IComponentData
	{
	}

	public struct MpVersusHeadOn : IGameMode, IReadWriteComponentSnapshot<MpVersusHeadOn, GhostSetup>
	{
		public struct Exclude : IComponentData
		{
		}

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
			OnMapEnd
		}

		public enum WinStatus
		{
			FlagCaptured,
			MorePoints,
			Forced
		}

		public State PlayState;
		public int   EndTime;

		public int       WinningTeam;
		public WinStatus WinReason;

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
			return ref UnsafeUtilityEx.ArrayElementAsRef<int>(UnsafeUtility.AddressOf(ref Team0Points), team);
		}

		public unsafe ref int GetEliminations(int team)
		{
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

			writer.WritePackedIntDelta(WinningTeam, baseline.WinningTeam, compression);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref MpVersusHeadOn baseline, DeserializeClientData jobData)
		{
			var compression = jobData.NetworkCompressionModel;

			PlayState = (State) reader.ReadPackedUIntDelta(ref ctx, (uint) baseline.PlayState, compression);
			EndTime   = reader.ReadPackedIntDelta(ref ctx, baseline.EndTime, compression);

			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, compression), out Team0);
			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, compression), out Team1);

			for (var i = 0; i != 2; i++)
			{
				GetPoints(i)       = reader.ReadPackedIntDelta(ref ctx, baseline.GetPointReadOnly(i), compression);
				GetEliminations(i) = reader.ReadPackedIntDelta(ref ctx, baseline.GetEliminationReadOnly(i), compression);
			}

			WinningTeam = reader.ReadPackedIntDelta(ref ctx, baseline.WinningTeam, compression);
		}
	}
}