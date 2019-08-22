using System.Collections.Generic;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.GameModes
{
	// 'Mp' indicate this is a MultiPlayer designed game-mode

	public class MpVersusHeadOnGameModeAsync : GameModeAsyncSystem<MpVersusHeadOn>
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
		private EntityQuery m_SpawnPointQuery;
		private EntityQuery m_FlagQuery;
		private EntityQuery m_PlayerWithoutGameModeDataQuery;
		private EntityQuery m_PlayerQuery;
		private EntityQuery m_UnitQuery;
		private EntityQuery m_LivableQuery;

		private EntityQuery m_EventOnCaptureQuery;
		private EntityQuery m_GameFormationQuery;

		protected override void OnCreateMachine(ref Machine machine)
		{
			machine.AddContext(new VersusHeadOnContext
			{
				ServerSimulationSystemGroup = World.GetOrCreateSystem<ServerSimulationSystemGroup>()
			});
			machine.SetCollection(new BlockOnceCollection("Execution", new List<Block>
			{
				new InitializationBlock("Init"),
				new BlockAutoLoopCollection("MapLoop", new List<Block>
				{
					new LoadMapBlock("LoadMap"),
					new StartMapBlock("StartMap"),
					new BlockAutoLoopCollection("RoundLoop", new List<Block>
					{
						new Block("StartRound"),
						new BlockAutoLoopCollection("GameLoop", new List<Block>
						{

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
			//EntityManager.SetComponentData(gameModeEntity, gmContext.Data);
		}

		public class InitializationBlock : Block
		{
			public const int TeamCount = 2;

			public WorldContext        WorldCtx;
			public VersusHeadOnContext GameModeCtx;

			public InitializationBlock(string name) : base(name)
			{
			}

			protected override bool OnRun()
			{
				// -- get world systems
				var teamProvider = WorldCtx.GetExistingSystem<GameModeTeamProvider>();
				var clubProvider = WorldCtx.GetExistingSystem<ClubProvider>();

				// -- Create teams
				GameModeCtx.Teams = new Team[TeamCount];
				for (var t = 0; t != TeamCount; t++)
				{
					ref var team = ref GameModeCtx.Teams[t];
					team.Target = teamProvider.SpawnLocalEntityWithArguments(new GameModeTeamProvider.Create());

					// Add club...
					var club = clubProvider.SpawnLocalEntityWithArguments(new ClubProvider.Create
					{
						name           = new NativeString64(t == 0 ? "Blue" : "Red"),
						primaryColor   = Color.Lerp(t == 0 ? Color.blue : Color.red, Color.white, 0.33f),
						secondaryColor = Color.Lerp(Color.Lerp(t == 0 ? Color.blue : Color.red, Color.white, 0.15f), Color.black, 0.15f)
					});
					WorldCtx.EntityMgr.AddComponentData(team.Target, new Relative<ClubDescription> {Target = club});
					WorldCtx.EntityMgr.AddComponent(club, typeof(GhostComponent));
				}

				// -- Set enemies of each team
				for (var t = 0; t != TeamCount; t++)
				{
					var enemies = WorldCtx.EntityMgr.GetBuffer<TeamEnemies>(GameModeCtx.Teams[t].Target);
					enemies.Add(new TeamEnemies {Target = GameModeCtx.Teams[1 - t].Target});
				}

				return true;
			}

			protected override void OnReset()
			{
				base.OnReset();

				// -------- -------- -------- -------- //
				// : Retrieve contexts
				// -------- -------- -------- -------- //
				GameModeCtx = Context.GetExternal<VersusHeadOnContext>();
				WorldCtx    = Context.GetExternal<WorldContext>();
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

		public class StartMapBlock : BlockCollection
		{
			public Block            SpawnBlock;
			public WaitingTickBlock ShowMapTextBlock;

			public StartMapBlock(string name) : base(name)
			{
				Add(SpawnBlock       = new Block("Test block"));
				Add(ShowMapTextBlock = new WaitingTickBlock("Time to show map start text"));
			}

			protected override bool OnRun()
			{
				if (RunNext(SpawnBlock))
				{
					ShowMapTextBlock.SetTicksFromMs(5000);

					return false;
				}

				if (RunNext(ShowMapTextBlock))
					return false;

				return true;
			}

			protected override void OnReset()
			{
				base.OnReset();

				ShowMapTextBlock.TickGetter = Context.GetExternal<VersusHeadOnContext>();
			}
		}
	}
}