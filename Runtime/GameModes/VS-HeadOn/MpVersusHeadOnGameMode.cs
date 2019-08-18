using System;
using DefaultNamespace;
using package.patapon.core;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer;
using Patapon4TLB.Default;
using Runtime.BaseSystems;
using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.Networking.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;

namespace Patapon4TLB.GameModes
{
	// 'Mp' indicate this is a MultiPlayer designed game-mode

	public class MpVersusHeadOnGameMode : GameModeSystem<MpVersusHeadOn>
	{
		public struct GameModeUnit : IComponentData
		{
			public int Team;
			public int FormationIndex;

			public UTick TickBeforeSpawn;
		}

		public struct GameModePlayer : IComponentData
		{

		}

		public struct Team
		{
			/// <summary>
			/// The team as an entity
			/// </summary>
			public Entity Target;

			/// <summary>
			/// The team spawn point
			/// </summary>
			public Entity SpawnPoint;

			/// <summary>
			/// The team flag
			/// </summary>
			public Entity Flag;
		}

		private EntityQuery m_UpdateTeamQuery;
		private EntityQuery m_SpawnPointQuery;
		private EntityQuery m_FlagQuery;
		private EntityQuery m_PlayerWithoutGameModeDataQuery;
		private EntityQuery m_PlayerQuery;
		private EntityQuery m_UnitQuery;

		private EntityQuery m_EventOnCaptureQuery;

		private EntityQuery m_GameFormationQuery;

		protected Team[]   Teams;
		protected Entity[] Structures;
		
		protected int WinningTeam;

		private UnitProvider m_UnitProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_UpdateTeamQuery                = GetEntityQuery(typeof(HeadOnTeamTarget), ComponentType.Exclude<Relative<TeamDescription>>());
			m_SpawnPointQuery                = GetEntityQuery(typeof(LocalToWorld), typeof(HeadOnSpawnPoint), typeof(HeadOnTeamTarget));
			m_FlagQuery                      = GetEntityQuery(typeof(LocalToWorld), typeof(HeadOnFlag), typeof(HeadOnTeamTarget));
			m_GameFormationQuery             = GetEntityQuery(typeof(GameFormationTag), typeof(FormationRoot));
			m_PlayerWithoutGameModeDataQuery = GetEntityQuery(typeof(GamePlayer), typeof(GamePlayerReadyTag), ComponentType.Exclude<GameModePlayer>());
			m_PlayerQuery                    = GetEntityQuery(typeof(GamePlayer), typeof(GamePlayerReadyTag));
			m_UnitQuery                      = GetEntityQuery(typeof(Translation), typeof(LivableHealth), typeof(GameModeUnit));
			m_EventOnCaptureQuery = GetEntityQuery(typeof(HeadOnStructureOnCapture));

			m_UnitProvider = World.GetOrCreateSystem<UnitProvider>();
		}

		public override unsafe void OnGameModeUpdate(Entity gmEntity, ref MpVersusHeadOn gameMode)
		{
			// ----------------------------- ----------------------------- //
			// > INIT PHASE
			// ----------------------------- ----------------------------- //
			if (IsInitialization())
			{
				FinishInitialization();

				WinningTeam = -1;

				// ----------------------------- //
				// Create teams
				Teams = new Team[2];

				var teamProvider = World.GetOrCreateSystem<GameModeTeamProvider>();
				var clubProvider = World.GetOrCreateSystem<ClubProvider>();
				
				var teamEntities = new NativeList<Entity>(2, Allocator.TempJob);

				// First pass, create team entities...
				for (var t = 0; t != Teams.Length; t++)
				{
					ref var team = ref Teams[t];
					teamProvider.SpawnLocalEntityWithArguments(new GameModeTeamProvider.Create
					{

					}, teamEntities);
					team.Target = teamEntities[t];

					// Add club...
					var club = clubProvider.SpawnLocalEntityWithArguments(new ClubProvider.Create
					{
						name           = new NativeString64(t == 0 ? "Blue" : "Red"),
						primaryColor   = Color.Lerp(t == 0 ? Color.blue : Color.red, Color.white, 0.33f),
						secondaryColor = Color.Lerp(Color.Lerp(t == 0 ? Color.blue : Color.red, Color.white, 0.15f), Color.black, 0.15f)
					});
					EntityManager.AddComponentData(team.Target, new Relative<ClubDescription>{Target = club});
					EntityManager.AddComponent(club, typeof(GhostComponent));
				}

				gameMode.Team0 = Teams[0].Target;
				gameMode.Team1 = Teams[1].Target;

				// Second pass, set enemies buffer
				for (var t = 0; t != Teams.Length; t++)
				{
					var enemies = EntityManager.GetBuffer<TeamEnemies>(Teams[t].Target);
					enemies.Add(new TeamEnemies {Target = Teams[1 - t].Target});
				}

				teamEntities.Dispose();

				// ----------------------------- //
				// Add players
				Entities.With(m_PlayerWithoutGameModeDataQuery).ForEach((Entity e) => { EntityManager.AddComponent(e, typeof(GameModePlayer)); });

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

			if (m_UpdateTeamQuery.CalculateEntityCount() > 0)
			{
				var entities = m_UpdateTeamQuery.ToEntityArray(Allocator.TempJob);
				var targetTeamArray = m_UpdateTeamQuery.ToComponentDataArray<HeadOnTeamTarget>(Allocator.TempJob);
				EntityManager.AddComponent(m_UpdateTeamQuery, typeof(Relative<TeamDescription>));
				for (var ent = 0; ent != entities.Length; ent++)
				{
					var target = targetTeamArray[ent];
					if (target.Custom != default)
						EntityManager.SetComponentData(entities[ent], new Relative<TeamDescription> {Target = target.Custom});
					else
					{
						EntityManager.SetComponentData(entities[ent], new Relative<TeamDescription> {Target = Teams[target.TeamIndex].Target});
					}
				}

				entities.Dispose();
				targetTeamArray.Dispose();
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
				// creating units...
				// creating rhythm engines...
				// or placing gimmicks...
				case MpVersusHeadOn.State.InitMap:
				{
					if (!MapManager.IsMapLoaded)
						return;

					// ----------------------------- //
					// Get spawn points
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
					
					// ----------------------------- //
					// Get flags
					using (var entities = m_FlagQuery.ToEntityArray(Allocator.TempJob))
					using (var teamTargetArray = m_FlagQuery.ToComponentDataArray<HeadOnTeamTarget>(Allocator.TempJob))
					{
						for (int ent = 0, length = entities.Length; ent < length; ent++)
						{
							var tTarget = teamTargetArray[ent];
							if (tTarget.TeamIndex < 0)
								continue;

							ref var team = ref Teams[tTarget.TeamIndex];
							team.Flag = entities[ent];
						}
					}

					// ----------------------------- //
					// Destroy previous rhythm engines
					EntityManager.DestroyEntity(Entities.WithAll<RhythmEngineDescription, Relative<PlayerDescription>>().ToEntityQuery());

					// ----------------------------- //
					// Set team of players
					// Create player rhythm engines
					// Create player unit targets
					Entities.With(m_PlayerQuery).ForEach((player) =>
					{
						var rhythmEngineProvider = World.GetOrCreateSystem<RhythmEngineProvider>();
						using (var spawnEntities = new NativeList<Entity>(Allocator.TempJob))
						{
							rhythmEngineProvider.SpawnLocalEntityWithArguments(new RhythmEngineProvider.Create
							{
								UseClientSimulation = true
							}, spawnEntities);

							EntityManager.SetOrAddComponentData(spawnEntities[0], EntityManager.GetComponentData<NetworkOwner>(player));
							EntityManager.SetOrAddComponentData(spawnEntities[0], new RhythmEngineProcess {StartTime = (int) World.GetExistingSystem<ServerSimulationSystemGroup>().GetTick().Ms});
							EntityManager.AddComponent(spawnEntities[0], typeof(GhostComponent));

							EntityManager.ReplaceOwnerData(spawnEntities[0], player);
						}

						var unitTarget = EntityManager.CreateEntity(typeof(UnitTargetDescription), typeof(Translation), typeof(LocalToWorld), typeof(Relative<PlayerDescription>));
						EntityManager.AddComponent(unitTarget, typeof(GhostComponent));
						EntityManager.ReplaceOwnerData(unitTarget, player);
					});

					// ----------------------------- //
					// Create units from formations
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
										var player = EntityManager.GetComponentData<Relative<PlayerDescription>>(units[unt].Value).Target;

										EntityManager.ReplaceOwnerData(ent, player);
										var cameraState = EntityManager.GetComponentData<ServerCameraState>(player);
										cameraState.Data.Target = ent;
										EntityManager.SetComponentData(player, cameraState);
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
										EntityManager.AddComponent(healthEntities[0], typeof(GhostComponent));
									}

									EntityManager.AddComponent(ent, typeof(UnitTargetControlTag));

									// create abilities...
									MasterServerAbilities.Convert(this, ent, EntityManager.GetBuffer<UnitDefinedAbilities>(units[unt].Value));
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
				// - reset target
				// - re-spawn players
				// - recreate map towers (and destructible entities)
				// - replacing gimmicks...
				case MpVersusHeadOn.State.RoundStart:
				{
					Entities.With(m_UnitQuery).ForEach((Entity e, ref Translation translation, ref GameModeUnit gameModeUnit, ref UnitDirection direction, ref LivableHealth livableHealth) =>
					{
						var spawnPosition = EntityManager.GetComponentData<LocalToWorld>(Teams[gameModeUnit.Team].SpawnPoint).Position;

						translation.Value   =  spawnPosition;
						translation.Value.x += gameModeUnit.FormationIndex * 0.75f * direction.Value;

						gameModeUnit.TickBeforeSpawn = new UTick {Value = -1};

						var healthEvent = EntityManager.CreateEntity(typeof(ModifyHealthEvent));
						EntityManager.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, e));

						if (EntityManager.HasComponent<Relative<UnitTargetDescription>>(e))
						{
							var targetRelative = EntityManager.GetComponentData<Relative<UnitTargetDescription>>(e);
							EntityManager.SetComponentData(targetRelative.Target, new Translation {Value = new float3(spawnPosition.x, float2.zero)});
						}

						livableHealth.IsDead = false;
					});

					WinningTeam = -1;

					gameMode.PlayState = MpVersusHeadOn.State.Playing;
					gameMode.EndTime   = ServerTick.Ms + GetSingleton<MpVersusHeadOnRule.Data>().TimeLimit;
					break;
				}
				
				// ----------------------------- //
				// GameMode Loop
				case MpVersusHeadOn.State.Playing:
				{
					World.GetExistingSystem<GameEventRuleSystemGroup>().Process();
					
					using (var entityArray = m_UnitQuery.ToEntityArray(Allocator.TempJob))
					using (var translationArray = m_UnitQuery.ToComponentDataArray<Translation>(Allocator.TempJob))
					using (var gmUnitArray = m_UnitQuery.ToComponentDataArray<GameModeUnit>(Allocator.TempJob))
					using (var healthArray = m_UnitQuery.ToComponentDataArray<LivableHealth>(Allocator.TempJob))
					{
						for (var ent = 0; ent != entityArray.Length; ent++)
						{
							ref var translation = ref UnsafeUtilityEx.ArrayElementAsRef<Translation>(translationArray.GetUnsafePtr(), ent);
							ref var gameModeUnit = ref UnsafeUtilityEx.ArrayElementAsRef<GameModeUnit>(gmUnitArray.GetUnsafePtr(), ent);
							ref var health = ref UnsafeUtilityEx.ArrayElementAsRef<LivableHealth>(healthArray.GetUnsafePtr(), ent);

							// ----------------------------- //
							// Update Units health state
							// If the unit should be dead, then set it dead
							if (health.Value <= 0 && !health.IsDead)
							{
								health.IsDead = true;

								ref var points = ref gameMode.GetPoints(1 - gameModeUnit.Team);
								points += 25;

								var vel = EntityManager.GetComponentData<Velocity>(entityArray[ent]);
								vel.Value += new float3(8 * (gameModeUnit.Team == 0 ? -1 : 1), 5, 0);
								EntityManager.SetComponentData(entityArray[ent], vel);

								// We only add an elimination if there was an instigator in the damage history
								var history       = EntityManager.GetBuffer<HealthModifyingHistory>(entityArray[ent]);
								var hasInstigator = false;
								for (var story = 0; !hasInstigator && story != history.Length; story++)
								{
									if (history[story].Instigator != default && history[story].Value < 0)
										hasInstigator = true;
								}

								if (hasInstigator)
								{
									ref var eliminations = ref gameMode.GetEliminations(1 - gameModeUnit.Team);
									eliminations++;
								}

								gameModeUnit.TickBeforeSpawn = UTick.AddMsNextFrame(ServerTick, GetSingleton<MpVersusHeadOnRule.Data>().RespawnTime);
							}

							// If the unit should respawn, then respawn him
							if (health.IsDead && gameModeUnit.TickBeforeSpawn <= ServerTick)
							{
								health.IsDead = false;
								
								var spawnPosition = EntityManager.GetComponentData<LocalToWorld>(Teams[gameModeUnit.Team].SpawnPoint).Position;
								translation.Value = spawnPosition;
								
								var healthEvent = EntityManager.CreateEntity(typeof(ModifyHealthEvent));
								EntityManager.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, entityArray[ent]));
							}
							
							var oppositeTeam   = Teams[1 - gameModeUnit.Team];
							var oppositeFlagTr = EntityManager.GetComponentData<Translation>(oppositeTeam.Flag).Value;
							var side           = gameModeUnit.Team == 0 ? -1 : 1;
							if ((oppositeFlagTr.x - translation.Value.x) * side >= 0)
							{
								Debug.Log($"{gameModeUnit.Team} captured the flag!");
								WinningTeam = gameModeUnit.Team;
							}
						}
						
						m_UnitQuery.CopyFromComponentDataArray(healthArray);
						m_UnitQuery.CopyFromComponentDataArray(gmUnitArray);
						m_UnitQuery.CopyFromComponentDataArray(translationArray);
					}
					
					// VS Events
					using (var onCaptureArray = m_EventOnCaptureQuery.ToComponentDataArray<HeadOnStructureOnCapture>(Allocator.TempJob))
					{
						for (var ent = 0; ent != onCaptureArray.Length; ent++)
						{
							var relativeTeam = EntityManager.GetComponentData<Relative<TeamDescription>>(onCaptureArray[ent].Source).Target;
							if (relativeTeam == default)
								continue;

							var structure = EntityManager.GetComponentData<HeadOnStructure>(onCaptureArray[ent].Source);
							var team      = relativeTeam == gameMode.Team0 ? 0 : 1;

							ref var points = ref gameMode.GetPoints(team);
							points += structure.Type == HeadOnStructure.EType.TowerControl ? 50 :
								structure.Type == HeadOnStructure.EType.Tower              ? 25 :
								                                                             10;
						}

						EntityManager.DestroyEntity(m_EventOnCaptureQuery);
					}

					if (WinningTeam == -1 && ServerTick.Ms > gameMode.EndTime)
					{
						WinningTeam = 0;
						for (var i = 0; i != Teams.Length; i++)
						{
							if (gameMode.GetPointReadOnly(i) > gameMode.GetPointReadOnly(1 - i))
							{
								WinningTeam = i + 1;
								break;
							}
						}	
					}
					
					if (WinningTeam >= 0)
						gameMode.PlayState = MpVersusHeadOn.State.RoundEnd;

					break;
				}
				case MpVersusHeadOn.State.RoundEnd:
					Debug.Log($"Winner! {WinningTeam}");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}