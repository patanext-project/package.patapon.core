using System;
using package.stormiumteam.shared;
using Patapon4TLB.Core.MasterServer;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	// 'Mp' indicate this is a MultiPlayer designed game-mode
	
	public struct MpVersusHeadOn : IGameMode
	{
		public struct RequestTag : IComponentData
		{
		}
		
		public enum State
		{
			GetImportantPlayerData,
			InitMap,
			RoundStart,
			Playing,
			RoundEnd
		}

		public State PlayState;
	}

	public class MpVersusHeadOnGameMode : GameModeSystem<MpVersusHeadOn>
	{
		public struct Team
		{
			/// <summary>
			/// The current match points
			/// </summary>
			public int MatchPoints;

			/// <summary>
			/// The current round points
			/// </summary>
			public int RoundPoints;

			/// <summary>
			/// The team as an entity
			/// </summary>
			public Entity Target;

			/// <summary>
			/// The team spawn point
			/// </summary>
			public Entity SpawnPoint;
		}

		private EntityQuery m_SpawnPointQuery;
		private EntityQuery m_PlayerQuery;

		private EntityQuery m_AllRequestQuery;
		private EntityQuery m_SuccessRequest;

		protected Team[]   Teams;
		protected Entity[] Structures;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_SpawnPointQuery = GetEntityQuery(typeof(LocalToWorld), typeof(HeadOnSpawnPoint), typeof(HeadOnTeamTarget));
			m_AllRequestQuery = GetEntityQuery(typeof(MpVersusHeadOn.RequestTag));
			m_SuccessRequest = GetEntityQuery(typeof(MpVersusHeadOn.RequestTag), typeof(RequestGetUserArmyData));
			m_PlayerQuery = GetEntityQuery(typeof(GamePlayer), typeof(GamePlayerReadyTag));
		}

		public override void OnGameModeUpdate(Entity entity, ref MpVersusHeadOn gameMode)
		{
			// ----------------------------- ----------------------------- //
			// > INIT PHASE
			// ----------------------------- ----------------------------- //
			if (IsInitialization())
			{
				FinishInitialization();

				// ----------------------------- //
				// Create teams
				Teams = new Team[2];

				var teamProvider = World.GetOrCreateSystem<GameModeTeamProvider>();
				var teamEntities = new NativeList<Entity>(2, Allocator.TempJob);

				// First pass, create team entities...
				for (var t = 0; t != Teams.Length; t++)
				{
					ref var team = ref Teams[t];
					teamProvider.SpawnLocalEntityWithArguments(new GameModeTeamProvider.Create
					{

					}, teamEntities);
					team.Target = teamEntities[t];
				}

				// Second pass, set enemies buffer
				for (var t = 0; t != Teams.Length; t++)
				{
					var enemies = EntityManager.GetBuffer<TeamEnemies>(Teams[t].Target);
					enemies.Add(new TeamEnemies {Target = Teams[1 - t].Target});
				}

				teamEntities.Dispose();

				// ----------------------------- //
				// Load map
				LoadMap();

				// ----------------------------- //
				// Set PlayState
				gameMode.PlayState = MpVersusHeadOn.State.GetImportantPlayerData;
			}

			// ----------------------------- ----------------------------- //
			// > CLEANUP PHASE
			// ----------------------------- ----------------------------- //
			if (IsCleanUp())
			{
				// Destroy request entities...
				EntityManager.DestroyEntity(m_AllRequestQuery);
				
				FinishCleanUp();
				return;
			}

			// ----------------------------- ----------------------------- //
			// > LOOP PHASE
			// ----------------------------- ----------------------------- //
			switch (gameMode.PlayState)
			{
				// Get from the important players their army information
				case MpVersusHeadOn.State.GetImportantPlayerData:
					using (var entities = m_PlayerQuery.ToEntityArray(Allocator.TempJob))
					{
						for (var ent = 0; ent != entities.Length; ent++)
						{
							var result = this.EntityManager.CreateEntity(typeof(MpVersusHeadOn), typeof(ResultGetUserArmyData));
							var armyBlob = ResultGetUserArmyData.ConstructBlob(Allocator.Persistent, new int[][]
							{
								new[]
								{
									0
								}
							});
						}
					}
					
					break;
				
				// ----------------------------- //
				// Init from map data
				//
				// This is where we will get the game towers, spawn points...
				// or placing gimmicks...
				//
				// This is also the place to load player data from MasterServer (if players aren't customized)
				case MpVersusHeadOn.State.InitMap:
					if (!MapManager.IsMapLoaded)
						return;
					
					// Get spawn points...
					using (var entities = m_SpawnPointQuery.ToEntityArray(Allocator.TempJob))
					using (var spawnDataArray = m_SpawnPointQuery.ToComponentDataArray<HeadOnSpawnPoint>(Allocator.TempJob))
					using (var teamTargetArray = m_SpawnPointQuery.ToComponentDataArray<HeadOnTeamTarget>(Allocator.TempJob))
					{
						for (int ent = 0, length = entities.Length; ent < length; ent++)
						{
							var tTarget = teamTargetArray[ent];
							if (tTarget.TeamIndex < 0)
								continue;

							ref var team = ref Teams[tTarget.TeamIndex];
							team.SpawnPoint = entities[ent];
						}
					}
					
					
					gameMode.PlayState = MpVersusHeadOn.State.RoundStart;
					break;

				// ----------------------------- //
				// On round start
				//
				// This is where we will:
				// - spawn players with required equipments and all
				// - recreate map towers (and destructible entities)
				// - replacing gimmicks...
				case MpVersusHeadOn.State.RoundStart:
					Debug.Log("Round start!");
					gameMode.PlayState = MpVersusHeadOn.State.Playing;
					break;
				case MpVersusHeadOn.State.Playing:
					break;
				case MpVersusHeadOn.State.RoundEnd:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}