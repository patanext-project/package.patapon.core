using DefaultNamespace;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	public struct MpVersusHeadOn : IGameMode, IComponentFromSnapshot<MpHeadOnGhostSerializer.MpHeadOnGameModeSnapshot>
	{
		public enum State
		{
			InitMap,
			RoundStart,
			Playing,
			RoundEnd
		}

		public State PlayState;
		public int   EndTime;

		public Entity Team0, Team1;

		// we can't use fixed buffer in burst???
		public int Team0Points;
		public int Team1Points;
		public int Team0Eliminations;
		public int Team1Eliminations;

		public unsafe void Set(MpHeadOnGhostSerializer.MpHeadOnGameModeSnapshot snapshot, NativeHashMap<int, GhostEntity> ghostMap)
		{
			PlayState = snapshot.PlayState;
			EndTime   = snapshot.EndTime;

			Team0Points       = snapshot.Team0Score;
			Team1Points       = snapshot.Team1Score;
			Team0Eliminations = snapshot.Team0Elimination;
			Team1Eliminations = snapshot.Team1Elimination;

			ghostMap.TryGetValue((int) snapshot.Team0GhostId, out var team0GhostEntity);
			Team0 = team0GhostEntity.entity;

			ghostMap.TryGetValue((int) snapshot.Team1GhostId, out var team1GhostEntity);
			Team1 = team1GhostEntity.entity;
		}

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
			
			fixed (int* t = &this.Team0Points)
			{
				return ref t[team];
			}
		}

		public unsafe ref int GetEliminations(int team)
		{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
			Debug.Assert(team >= 0 && team <= 1, "team >= 0 && team <= 1");
#endif
			return ref UnsafeUtilityEx.ArrayElementAsRef<int>(UnsafeUtility.AddressOf(ref Team0Eliminations), team);
			
			fixed (int* t = &this.Team0Eliminations)
			{
				return ref t[team];
			}
		}
	}
}