using System;
using package.stormiumteam.shared;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;

namespace Patapon4TLB.GameModes
{
	// 'Mp' indicate this is a MultiPlayer designed game-mode

	public struct MpVersusHeadOn : IGameMode
	{
		public enum State
		{
			InitMap,
			RoundStart,
			Playing,
			RoundEnd
		}

		public State PlayState;
	}

	public unsafe class MpVersusHeadOnGameMode : GameModeSystem<MpVersusHeadOn>
	{
		public struct GameModeUnit : IComponentData
		{
			public int Team;
			public int FormationIndex;

			public int TickBeforeSpawn;
		}

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
		private EntityQuery m_UnitQuery;

		private EntityQuery m_GameFormationQuery;

		protected Team[]   Teams;
		protected Entity[] Structures;

		private UnitProvider m_UnitProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_SpawnPointQuery    = GetEntityQuery(typeof(LocalToWorld), typeof(HeadOnSpawnPoint), typeof(HeadOnTeamTarget));
			m_GameFormationQuery = GetEntityQuery(typeof(GameFormationTag), typeof(FormationRoot));
			m_PlayerQuery        = GetEntityQuery(typeof(GamePlayer), typeof(GamePlayerReadyTag));
			m_UnitQuery          = GetEntityQuery(typeof(GameModeUnit));

			m_UnitProvider = World.GetOrCreateSystem<UnitProvider>();
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
				gameMode.PlayState = MpVersusHeadOn.State.InitMap;
			}

			// ----------------------------- ----------------------------- //
			// > CLEANUP PHASE
			// ----------------------------- ----------------------------- //
			if (IsCleanUp())
			{
				FinishCleanUp();
				return;
			}

			// ----------------------------- ----------------------------- //
			// > LOOP PHASE
			// ----------------------------- ----------------------------- //
			switch (gameMode.PlayState)
			{
				// ----------------------------- //
				// Init from map data
				//
				// This is where we will get the game towers, spawn points...
				// creating players...
				// or placing gimmicks...
				case MpVersusHeadOn.State.InitMap:
				{
					if (!MapManager.IsMapLoaded)
						return;

					// Get spawn points...
					using (var entities = m_SpawnPointQuery.ToEntityArray(Allocator.TempJob))
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

					using (var spawnedUnits = new NativeList<Entity>(Allocator.TempJob))
					using (var entities = m_GameFormationQuery.ToEntityArray(Allocator.TempJob))
					{
						for (var form = 0; form != entities.Length; form++)
						{
							var team = EntityManager.GetComponentData<FormationTeam>(entities[form]);
							if (team.TeamIndex == 0)
								continue;

							var armies = EntityManager.GetBuffer<FormationChild>(entities[form]).ToNativeArray(Allocator.TempJob);
							for (var arm = 0; arm != armies.Length; arm++)
							{
								var units = EntityManager.GetBuffer<FormationChild>(armies[arm].Value).ToNativeArray(Allocator.TempJob);
								for (var unt = 0; unt != units.Length; unt++)
								{
									if (!EntityManager.HasComponent<UnitFormation>(units[unt].Value))
										continue;

									var capsuleColl = CapsuleCollider.Create(0, math.up() * 2, 0.5f);
									m_UnitProvider.SpawnLocalEntityWithArguments(new UnitProvider.Create
									{
										Direction       = team.TeamIndex == 1 ? UnitDirection.Right : UnitDirection.Left,
										MovableCollider = capsuleColl,
										Mass            = PhysicsMass.CreateKinematic(capsuleColl.Value.MassProperties),
										Settings        = EntityManager.GetComponentData<UnitStatistics>(units[unt].Value)
									}, spawnedUnits);

									var ent = spawnedUnits[spawnedUnits.Length - 1];
									EntityManager.AddComponent(ent, typeof(GhostComponent));
									EntityManager.AddComponentData(ent, new Relative<TeamDescription> {Target = Teams[team.TeamIndex - 1].Target});
									EntityManager.AddComponentData(ent, new GameModeUnit
									{
										Team           = team.TeamIndex - 1,
										FormationIndex = arm
									});

									if (EntityManager.HasComponent<Relative<PlayerDescription>>(units[unt].Value))
									{
										EntityManager.AddComponentData(ent, EntityManager.GetComponentData<Relative<PlayerDescription>>(units[unt].Value));
									}

									// create health entities...
									var healthProvider = World.GetExistingSystem<DefaultHealthData.InstanceProvider>();
									using (var healthEntities = new NativeList<Entity>(Allocator.TempJob))
									{
										var stat = EntityManager.GetComponentData<UnitStatistics>(units[unt].Value);
										healthProvider.SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance
										{
											max   = stat.Health,
											value = stat.Health,
											owner = ent
										}, healthEntities);
									}
								}

								units.Dispose();
							}

							armies.Dispose();
						}
					}

					gameMode.PlayState = MpVersusHeadOn.State.RoundStart;
					break;
				}

				// ----------------------------- //
				// On round start
				//
				// This is where we will:
				// - re-spawn players
				// - recreate map towers (and destructible entities)
				// - replacing gimmicks...
				case MpVersusHeadOn.State.RoundStart:
				{
					Debug.Log("Round start!");

					Entities.With(m_UnitQuery).ForEach((Entity e, ref Translation translation, ref GameModeUnit gameModeUnit) =>
					{
						var spawnPosition = EntityManager.GetComponentData<LocalToWorld>(Teams[gameModeUnit.Team].SpawnPoint).Position;

						translation.Value   =  spawnPosition;
						translation.Value.x += gameModeUnit.FormationIndex * 0.75f;

						gameModeUnit.TickBeforeSpawn = -1;

						var healthEvent = EntityManager.CreateEntity(typeof(ModifyHealthEvent));
						EntityManager.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, e));
					});

					gameMode.PlayState = MpVersusHeadOn.State.Playing;
					break;
				}

				case MpVersusHeadOn.State.Playing:
					
					Entities.With(m_UnitQuery).ForEach((Entity e, ref LivableHealth health) =>
					{
						var healthEvent = EntityManager.CreateEntity(typeof(ModifyHealthEvent));
						EntityManager.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.Add, -1, e));
					});
					
					break;
				case MpVersusHeadOn.State.RoundEnd:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}