using System;
using System.Collections.Generic;
using GmMachine;
using GmMachine.Blocks;
using GmMachine.Blocks.Instructions;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using Patapon4TLB.Core;
using Patapon4TLB.Default;
using Patapon4TLBCore;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Revolution.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Patapon4TLB.GameModes
{
	// 'Mp' indicate this is a MultiPlayer designed game-mode
	public class VersusHeadOnRuleGroup : RuleSystemGroupBase
	{}
	
	public partial class MpVersusHeadOnGameMode : GameModeAsyncSystem<MpVersusHeadOn>
	{
		public struct OnUnitElimination : IComponentData
		{
			public int EntityTeam;
			public int InstigatorTeam;
			
			public Entity Entity;
			public Entity Instigator;
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

			public int AveragePower;
		}

		public interface IEntityQueryBuilderGetter
		{
			EntityQueryBuilder GetEntityQueryBuilder();
		}

		public class VersusHeadOnQueriesContext : ExternalContextBase, IEntityQueryBuilderGetter
		{
			public MpVersusHeadOnGameMode GameModeSystem;

			public EntityQuery UpdateTeam;
			public EntityQuery SpawnPoint;
			public EntityQuery Flag;
			public EntityQuery PlayerWithoutGameModeData;
			public EntityQuery Player;

			public EntityQuery Formation;
			public EntityQuery Unit;

			public EntityQueryBuilder GetEntityQueryBuilder()
			{
				return GameModeSystem.Entities;
			}
		}

		public class VersusHeadOnContext : ExternalContextBase, ITickGetter
		{
			public MpVersusHeadOnGameMode      GameModeSystem;
			public ServerSimulationSystemGroup ServerSimulationSystemGroup;

			public Team[]         Teams;
			public Entity         Entity;
			public MpVersusHeadOn Data;

			public NativeList<OnUnitElimination>        EliminationEvents => GameModeSystem.EliminationEvents;
			public NativeList<HeadOnStructureOnCapture> CaptureEvents     => GameModeSystem.CaptureEvents;

			public UTick GetTick()
			{
				return ServerSimulationSystemGroup.GetTick();
			}
		}

		public NativeList<OnUnitElimination> EliminationEvents;
		public NativeList<HeadOnStructureOnCapture> CaptureEvents;

		private EntityQuery m_UnitQuery;
		private EntityQuery m_LivableQuery;

		private EntityQuery m_EventOnCaptureQuery;

		protected override void OnCreateMachine(ref Machine machine)
		{
			EliminationEvents = new NativeList<OnUnitElimination>(Allocator.Persistent);
			CaptureEvents = new NativeList<HeadOnStructureOnCapture>(Allocator.Persistent);
			
			machine.AddContext(new VersusHeadOnContext
			{
				GameModeSystem = this,
				
				ServerSimulationSystemGroup = World.GetOrCreateSystem<ServerSimulationSystemGroup>(),
				Teams                       = new Team[2]
			});
			machine.AddContext(new VersusHeadOnQueriesContext
			{
				GameModeSystem = this,
				UpdateTeam = GetEntityQuery(new EntityQueryDesc
				{
					All  = new ComponentType[] {typeof(HeadOnTeamTarget)},
					None = new ComponentType[] {typeof(Relative<TeamDescription>)}
				}),
				Flag = GetEntityQuery(new EntityQueryDesc
				{
					All = new ComponentType[] {typeof(LocalToWorld), typeof(HeadOnFlag), typeof(HeadOnTeamTarget)}
				}),
				SpawnPoint = GetEntityQuery(new EntityQueryDesc
				{
					All = new ComponentType[] {typeof(LocalToWorld), typeof(HeadOnSpawnPoint), typeof(HeadOnTeamTarget)}
				}),
				PlayerWithoutGameModeData = GetEntityQuery(new EntityQueryDesc
				{
					All  = new ComponentType[] {typeof(GamePlayer), typeof(GamePlayerReadyTag)},
					None = new ComponentType[] {typeof(VersusHeadOnPlayer)}
				}),
				Player = GetEntityQuery(new EntityQueryDesc
				{
					All = new ComponentType[] {typeof(GamePlayer), typeof(GamePlayerReadyTag)}
				}),
				Formation = GetEntityQuery(new EntityQueryDesc
				{
					All = new ComponentType[] {typeof(GameFormationTag), typeof(FormationRoot)}
				}),
				Unit = GetEntityQuery(new EntityQueryDesc
				{
					All = new ComponentType[] {typeof(UnitDescription), typeof(VersusHeadOnUnit)}
				})
			});
			machine.SetCollection(new InstantChainLoopBlock("Execution", new List<Block>
			{
				// off operations
				new InstructionCollection("Off-operations", new List<Block>
				{
					new SetStructureTeamRelative("Update Team Relatives of structure")
				}),

				// main operations
				new BlockOnceCollection("VersusHeadOn GameMode", new List<Block>
				{
					new SetStateBlock("Set state to Init", MpVersusHeadOn.State.OnInitialization),
					new InitializationBlock("Init"),
					new BlockAutoLoopCollection("MapLoop", new List<Block>
					{
						// -- On Map Load
						new InstantChainLoopBlock("Don't skip frame", new List<Block>
						{
							new SetStateBlock("Set state to map loading", MpVersusHeadOn.State.OnLoadingMap),
							new LoadMapBlock("LoadMap")
						}),
						// -- On Map loaded
						new InstantChainLoopBlock("Don't skip frame", new List<Block>
						{
							new SetStateBlock("Set state to start map", MpVersusHeadOn.State.OnMapStart),
							new StartMapBlock("StartMap")
						}),
						// -- Round Loop
						new BlockAutoLoopCollection("RoundLoop", new List<Block>
						{
							// -- On round started
							new InstantChainLoopBlock("Don't skip frame", new List<Block>
							{
								new SetStateBlock("Set state to round start", MpVersusHeadOn.State.OnRoundStart),
								new StartRoundBlock("StartRound")
							}),
							// -- Game Loop
							new BlockAutoLoopCollection("GameLoop", new List<Block>
							{
								// -- On Loop started
								new SetStateBlock("Set state to game loop", MpVersusHeadOn.State.Playing),
								new PlayLoopBlock("PlayLoop"),
							})
							{
								ResetOnBeginning = true
							},
							new Block("EndRound")
						})
						{
							ResetOnBeginning = true
						},
						new Block("EndMap"),
						new Block("UnloadMap")
					})
					{
						ResetOnBeginning = true
					}
				})
			}));
		}

		protected override void OnLoop(Entity gameModeEntity)
		{
			Machine.Update();
			var gmContext = Machine.GetContext<VersusHeadOnContext>();
			{
				gmContext.Data.Team0 = gmContext.Teams[0].Target;
				gmContext.Data.Team1 = gmContext.Teams[1].Target;
			}
			EntityManager.SetComponentData(gameModeEntity, gmContext.Data);
		}

		public class SetStructureTeamRelative : Block
		{
			private WorldContext               m_WorldContext;
			private VersusHeadOnContext        m_GameModeContext;
			private VersusHeadOnQueriesContext m_QueriesContext;

			public SetStructureTeamRelative(string name) : base(name)
			{
			}

			protected override bool OnRun()
			{
				var query = m_QueriesContext.UpdateTeam;
				if (query.CalculateEntityCount() > 0)
				{
					var entityManager   = m_WorldContext.EntityMgr;
					var entities        = query.ToEntityArray(Allocator.TempJob);
					var targetTeamArray = query.ToComponentDataArray<HeadOnTeamTarget>(Allocator.TempJob);
					entityManager.AddComponent(query, typeof(Relative<TeamDescription>));
					for (var ent = 0; ent != entities.Length; ent++)
					{
						var target = targetTeamArray[ent];
						if (target.Custom != default)
							entityManager.SetComponentData(entities[ent], new Relative<TeamDescription> {Target = target.Custom});
						else
						{
							entityManager.SetComponentData(entities[ent], new Relative<TeamDescription> {Target = m_GameModeContext.Teams[target.TeamIndex].Target});
						}
					}

					entities.Dispose();
					targetTeamArray.Dispose();
				}

				return true;
			}

			protected override void OnReset()
			{
				base.OnReset();

				m_QueriesContext  = Context.GetExternal<VersusHeadOnQueriesContext>();
				m_GameModeContext = Context.GetExternal<VersusHeadOnContext>();
				m_WorldContext    = Context.GetExternal<WorldContext>();
			}
		}

		public class SetStateBlock : Block
		{
			private VersusHeadOnContext m_HeadOnCtx;

			public readonly MpVersusHeadOn.State State;

			public SetStateBlock(string name, MpVersusHeadOn.State state) : base(name)
			{
				State = state;
			}

			protected override bool OnRun()
			{
				m_HeadOnCtx.Data.PlayState = State;
				return true;
			}

			protected override void OnReset()
			{
				base.OnReset();

				m_HeadOnCtx = Context.GetExternal<VersusHeadOnContext>();
			}
		}

		public class LoadMapBlock : Block
		{
			private WorldContext    m_WorldCtx;
			private GameModeContext m_GameModeCtx;
			private Entity          m_RequestEntity;

			public LoadMapBlock(string name) : base(name)
			{
			}

			protected override bool OnRun()
			{
				if (m_RequestEntity != default)
					return m_GameModeCtx.IsMapLoaded;

				m_RequestEntity = m_WorldCtx.EntityMgr.CreateEntity(typeof(RequestMapLoad));
				{
					m_WorldCtx.EntityMgr.SetComponentData(m_RequestEntity, new RequestMapLoad {Key = new NativeString512("testvs")});
				}

				return false;
			}

			protected override void OnReset()
			{
				base.OnReset();

				m_RequestEntity = Entity.Null;
				m_WorldCtx      = Context.GetExternal<WorldContext>();
				m_GameModeCtx   = Context.GetExternal<GameModeContext>();
			}
		}
		
		private static class Utility
		{public static void CreateUnitsBase(ComponentSystemBase       dummySystem,
			                             World                     worldOrigin,      EntityQuery        formationQuery,
			                             Func<Entity, World, bool> isFormationValid, Func<Entity, bool> isArmyValid, Action<Entity, int, Entity, int, Entity, World> onEntityCreated)
			{
				var unitProvider = worldOrigin.GetExistingSystem<UnitProvider>();

				var entityMgr             = worldOrigin.EntityManager;
				var wasFormationQueryNull = formationQuery == null;
				if (wasFormationQueryNull)
				{
					formationQuery = entityMgr.CreateEntityQuery(typeof(GameFormationTag), typeof(FormationRoot));
				}

				using (var entities = formationQuery.ToEntityArray(Allocator.TempJob))
				{
					for (var form = 0; form != entities.Length; form++)
					{
						var team = entityMgr.GetComponentData<FormationTeam>(entities[form]);
						if (!isFormationValid(entities[form], worldOrigin))
							continue;

						var armies = entityMgr.GetBuffer<FormationChild>(entities[form]).ToNativeArray(Allocator.TempJob);
						for (var arm = 0; arm != armies.Length; arm++)
						{
							if (!isArmyValid(armies[arm].Value))
								continue;

							var units = entityMgr.GetBuffer<FormationChild>(armies[arm].Value).ToNativeArray(Allocator.TempJob);
							for (var unt = 0; unt != units.Length; unt++)
							{
								var capsuleColl = Unity.Physics.CapsuleCollider.Create(0, math.up() * 2, 0.5f);
								var spawnedUnit = unitProvider.SpawnLocalEntityWithArguments(new UnitProvider.Create
								{
									Direction       = team.TeamIndex <= 1 ? UnitDirection.Right : UnitDirection.Left,
									MovableCollider = capsuleColl,
									Mass            = PhysicsMass.CreateKinematic(capsuleColl.Value.MassProperties),
									Settings        = entityMgr.GetComponentData<UnitStatistics>(units[unt].Value)
								});

								entityMgr.AddComponent(spawnedUnit, typeof(GhostComponent));
								if (entityMgr.HasComponent<Relative<PlayerDescription>>(units[unt].Value))
								{
									entityMgr.ReplaceOwnerData(spawnedUnit, entityMgr.GetComponentData<Relative<PlayerDescription>>(units[unt].Value).Target);
								}
								else
								{
									entityMgr.AddComponent(spawnedUnit, typeof(BotControlledUnit));
								}

								var stat = entityMgr.GetComponentData<UnitStatistics>(units[unt].Value);
								var healthEntity = worldOrigin.GetExistingSystem<DefaultHealthData.InstanceProvider>().SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance
								{
									max   = stat.Health,
									value = stat.Health,
									owner = spawnedUnit
								});
								entityMgr.AddComponent(healthEntity, typeof(GhostComponent));
								MasterServerAbilities.Convert(dummySystem, spawnedUnit, entityMgr.GetBuffer<UnitDefinedAbilities>(units[unt].Value));

								onEntityCreated(entities[form], form, armies[arm].Value, arm, spawnedUnit, worldOrigin);
							}
						}
					}
				}

				if (wasFormationQueryNull)
					formationQuery.Dispose();
			}	
		}
	}
}