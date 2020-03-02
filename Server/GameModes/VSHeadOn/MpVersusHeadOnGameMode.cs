using System.Collections.Generic;
using GmMachine;
using GmMachine.Blocks;
using GmMachine.Blocks.Instructions;
using Misc.GmMachine.Blocks;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.Units;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.External;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Patapon.Server.GameModes.VSHeadOn
{
	// 'Mp' indicate this is a MultiPlayer designed game-mode
	public class VersusHeadOnRuleGroup : RuleSystemGroupBase
	{
	}

	public class MpVersusHeadOnGameMode : GameModeAsyncSystem<MpVersusHeadOn>
	{
		public NativeList<HeadOnStructureOnCapture> CaptureEvents;

		public NativeList<HeadOnOnUnitElimination> UnitEliminationEvents;
		public NativeList<HeadOnOnDestroyArea>     DestroyAreaEvents;
		public NativeList<Entity>                  RespawnEvents;

		private EntityQuery m_EventOnCaptureQuery;
		private EntityQuery m_LivableQuery;

		private EntityQuery m_UnitQuery;

		protected override void OnCreateMachine(ref Machine machine)
		{
			UnitEliminationEvents = new NativeList<HeadOnOnUnitElimination>(Allocator.Persistent);
			DestroyAreaEvents     = new NativeList<HeadOnOnDestroyArea>(Allocator.Persistent);
			CaptureEvents         = new NativeList<HeadOnStructureOnCapture>(Allocator.Persistent);
			RespawnEvents         = new NativeList<Entity>(Allocator.Persistent);

			machine.AddContext(new ModeContext
			{
				RunPreMatch    = false,
				GameModeSystem = this,
				
				ServerSimulationSystemGroup = World.GetOrCreateSystem<ServerSimulationSystemGroup>(),
				Teams                       = new MpVersusHeadOnTeam[2]
			});
			machine.AddContext(new QueriesContext
			{
				GameModeSystem = this,
				Builder = machine.GetContext<QueryBuilderContext>(),
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
					All = new ComponentType[] {typeof(UnitDescription), typeof(Translation), typeof(VersusHeadOnUnit)}
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
				new BlockAutoLoopCollection("VersusHeadOn GameMode", new List<Block>
				{
					new SetStateBlock("Set state to Init", MpVersusHeadOn.State.OnInitialization),
					new InitializationBlock("Init"),
					new BlockAutoLoopCollection("MapLoop", new List<Block>
					{
						// Always do prematch!
						new PreMatchBlock("PreMatch"),
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
								new PlayLoopBlock("PlayLoop")
							})
							{
								ResetOnBeginning = true
							},
							new EndRoundBlock("EndRound")
						})
						{
							ResetOnBeginning = true
						},
						new EndMapBlock("EndMap"),
						new UnloadMapBlock("UnloadMap")
					})
					{
						ResetOnBeginning = true
					}
				})
			}));
		}

		protected override void OnLoop(Entity gameModeEntity)
		{
			var gmContext = Machine.GetContext<ModeContext>();
			if (IsInitialization())
			{
				EntityManager.SetOrAddComponentData(gameModeEntity, new GameModeHudSettings());
				FinishInitialization();

				if (HasSingleton<GameModePreMatch>())
					gmContext.RunPreMatch = true;
			}

			Machine.Update();
			{
				gmContext.Data.Team0 = gmContext.Teams[0].Target;
				gmContext.Data.Team1 = gmContext.Teams[1].Target;
			}
			EntityManager.SetComponentData(gameModeEntity, gmContext.Data);
			EntityManager.SetComponentData(gameModeEntity, gmContext.HudSettings);
		}

		public interface IEntityQueryBuilderGetter
		{
			EntityQueryBuilder GetEntityQueryBuilder();
		}

		public class QueriesContext : ExternalContextBase, IEntityQueryBuilderGetter
		{
			public EntityQuery Flag;

			public EntityQuery            Formation;
			public MpVersusHeadOnGameMode GameModeSystem;
			public QueryBuilderContext    Builder;
			public EntityQuery            Player;
			public EntityQuery            PlayerWithoutGameModeData;
			public EntityQuery            SpawnPoint;
			public EntityQuery            Unit;

			public EntityQuery UpdateTeam;

			public EntityQueryBuilder GetEntityQueryBuilder()
			{
				return Builder.From;
			}
		}

		public class ModeContext : ExternalContextBase, ITickGetter
		{
			public bool RunPreMatch;
			public int RoundPerMatch;
			
			public MpVersusHeadOn              Data;
			public Entity                      Entity;
			public MpVersusHeadOnGameMode      GameModeSystem;
			public ServerSimulationSystemGroup ServerSimulationSystemGroup;

			public MpVersusHeadOnTeam[] Teams;
			public GameModeHudSettings  HudSettings;

			public NativeList<HeadOnOnUnitElimination>  UnitEliminationEvents => GameModeSystem.UnitEliminationEvents;
			public NativeList<HeadOnOnDestroyArea>  DestroyAreaEvents => GameModeSystem.DestroyAreaEvents;
			public NativeList<HeadOnStructureOnCapture> CaptureEvents         => GameModeSystem.CaptureEvents;
			public NativeList<Entity>                   RespawnEvents         => GameModeSystem.RespawnEvents;

			public UTick GetTick()
			{
				return ServerSimulationSystemGroup.GetServerTick();
			}
		}
	}

	public struct HeadOnOnUnitElimination : IComponentData
	{
		public int EntityTeam;
		public int InstigatorTeam;

		public Entity Entity;
		public Entity Instigator;
	}

	public struct HeadOnOnDestroyArea : IComponentData
	{
		public Entity EntityTeam;
		public Entity InstigatorTeam;

		public Entity Entity;
		public Entity Instigator;

		public int Score;
	}

	public struct MpVersusHeadOnTeam
	{
		/// <summary>
		///     The team as an entity
		/// </summary>
		public Entity Target;

		/// <summary>
		///     The team spawn point
		/// </summary>
		public Entity SpawnPoint;

		/// <summary>
		///     The team flag
		/// </summary>
		public Entity Flag;

		public int AveragePower;
	}
}