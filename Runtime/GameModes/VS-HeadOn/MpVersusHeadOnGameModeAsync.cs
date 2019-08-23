using System.Collections.Generic;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	// 'Mp' indicate this is a MultiPlayer designed game-mode
	public partial class MpVersusHeadOnGameModeAsync : GameModeAsyncSystem<MpVersusHeadOn>
	{
		public struct GameModeUnit : IComponentData
		{
			public int Team;
			public int FormationIndex;

			public int KillStreak;
			public int DeadCount;

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

		public interface IEntityQueryBuilderGetter
		{
			EntityQueryBuilder GetEntityQueryBuilder();
		}

		public class VersusHeadOnQueriesContext : ExternalContextBase, IEntityQueryBuilderGetter
		{
			public MpVersusHeadOnGameModeAsync GameModeSystem;

			public EntityQuery SpawnPoint;
			public EntityQuery Flag;
			public EntityQuery PlayerWithoutGameModeData;
			public EntityQuery Player;

			public EntityQueryBuilder GetEntityQueryBuilder()
			{
				return GameModeSystem.Entities;
			}
		}

		public class VersusHeadOnContext : ExternalContextBase, ITickGetter
		{
			public ServerSimulationSystemGroup ServerSimulationSystemGroup;

			public Team[]         Teams;
			public Entity         Entity;
			public MpVersusHeadOn Data;


			public UTick GetTick()
			{
				return ServerSimulationSystemGroup.GetTick();
			}
		}

		private EntityQuery m_UpdateTeamQuery;
		private EntityQuery m_UnitQuery;
		private EntityQuery m_LivableQuery;

		private EntityQuery m_EventOnCaptureQuery;
		private EntityQuery m_GameFormationQuery;

		protected override void OnCreateMachine(ref Machine machine)
		{
			machine.AddContext(new VersusHeadOnContext
			{
				ServerSimulationSystemGroup = World.GetOrCreateSystem<ServerSimulationSystemGroup>(),
				Teams = new Team[2]
			});
			machine.AddContext(new VersusHeadOnQueriesContext
			{
				GameModeSystem = this,
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
					None = new ComponentType[] {typeof(GameModePlayer)}
				}),
				Player = GetEntityQuery(new EntityQueryDesc
				{
					All = new ComponentType[] {typeof(GamePlayer), typeof(GamePlayerReadyTag)}
				})
			});
			machine.SetCollection(new BlockOnceCollection("Execution", new List<Block>
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
							new Block("StartRound")
						}),
						// -- Game Loop
						new BlockAutoLoopCollection("GameLoop", new List<Block>
						{
							// -- On Loop started
							new SetStateBlock("Set state to game loop", MpVersusHeadOn.State.Playing)
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
	}
}