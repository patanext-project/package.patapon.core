using System.Collections.Generic;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.Training;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Units;
using Patapon.Server.GameModes.VSHeadOn;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Server.GameModes.Training
{
	public class TrainingGameMode : GameModeAsyncSystem<SoloTraining>
	{
		protected override void OnCreateMachine(ref Machine machine)
		{
			machine.AddContext(new ModeContext
			{
				SimulationGroup = World.GetExistingSystem<ServerSimulationSystemGroup>()
			});
			machine.AddContext(new QueryContext
			{
				Player = GetEntityQuery(typeof(GamePlayer)),
				System = this
			});
			machine.SetCollection(new BlockAutoLoopCollection("GameLoop", new List<Block>
			{
				new BlockAutoLoopCollection("MapLoop", new List<Block>
				{
					new LoadMap(),
					new WaitForPlayer(),
					new Init(),
					new BlockAutoLoopCollection("PlayLoop", new List<Block>
					{
						new PlayLoopBlock()
					})
				})
			}));
		}

		protected override void OnLoop(Entity gameModeEntity)
		{
			if (IsInitialization())
			{
				EntityManager.SetOrAddComponentData(gameModeEntity, new GameModeHudSettings
				{
					EnableUnitSounds        = true,
					EnableGameModeInterface = true
				});
				FinishInitialization();
			}

			Machine.Update();
		}

		public class LoadMap : Block
		{
			private WorldContext m_WorldCtx;
			private GameModeContext m_GameModeCtx;
			
			private Entity m_RequestEntity;
			
			protected override bool OnRun()
			{			
				if (m_RequestEntity != default)
					return m_GameModeCtx.IsMapLoaded;

				m_RequestEntity = m_WorldCtx.EntityMgr.CreateEntity(typeof(RequestMapLoad));
				{
					m_WorldCtx.EntityMgr.SetComponentData(m_RequestEntity, new RequestMapLoad {Key = new NativeString512("training_room")});
				}

				return false;
			}

			protected override void OnReset()
			{
				base.OnReset();

				m_WorldCtx = Context.GetExternal<WorldContext>();
				m_GameModeCtx = Context.GetExternal<GameModeContext>();
			}
		}

		public class WaitForPlayer : Block
		{
			private QueryContext m_Queries;
			
			protected override bool OnRun()
			{
				return !m_Queries.Player.IsEmptyIgnoreFilter;
			}

			protected override void OnReset()
			{
				base.OnReset();

				m_Queries = Context.GetExternal<QueryContext>();
			}
		}

		public class Init : Block
		{
			private QueryContext m_Queries;
			private WorldContext m_WorldCtx;
			private ModeContext  m_GmContext;

			protected override bool OnRun()
			{
				// ----------------------------- //
				// Set team of players
				// Create player rhythm engines
				// Create player unit targets
				m_Queries.GetBuilder().With(m_Queries.Player).ForEach(player =>
				{
					var entMgr = m_WorldCtx.EntityMgr;
					// Player without NetworkOwner mean that it's a bot.
					if (entMgr.TryGetComponentData(player, out NetworkOwner networkOwner))
					{
						var rhythmEngineProvider = m_WorldCtx.GetOrCreateSystem<RhythmEngineProvider>();
						var rhythmEnt = rhythmEngineProvider.SpawnLocalEntityWithArguments(new RhythmEngineProvider.Create
						{
							UseClientSimulation = true
						});

						entMgr.SetOrAddComponentData(rhythmEnt, networkOwner);
						entMgr.SetOrAddComponentData(rhythmEnt, new FlowEngineProcess {StartTime = m_GmContext.GetTick().Ms});
						entMgr.AddComponent(rhythmEnt, typeof(GhostEntity));
						entMgr.AddComponentData(rhythmEnt, new OwnerServerId {Value = m_WorldCtx.EntityMgr.GetComponentData<GamePlayer>(player).ServerId});

						entMgr.ReplaceOwnerData(rhythmEnt, player);
					}

					var unitTarget = entMgr.CreateEntity(typeof(UnitTargetDescription), typeof(Translation), typeof(LocalToWorld), typeof(Relative<PlayerDescription>));
					entMgr.AddComponent(unitTarget, typeof(GhostEntity));
					entMgr.AddComponentData(unitTarget, EntityDescription.New<UnitTargetDescription>());
					entMgr.ReplaceOwnerData(unitTarget, player);
				});
				return true;
			}

			protected override void OnReset()
			{
				base.OnReset();

				m_Queries   = Context.GetExternal<QueryContext>();
				m_WorldCtx  = Context.GetExternal<WorldContext>();
				m_GmContext = Context.GetExternal<ModeContext>();
			}
		}

		public class PlayLoopBlock : Block
		{
			protected override bool OnRun()
			{
				
				return true;
			}
		}

		public class ModeContext : ExternalContextBase, ITickGetter
		{
			public ServerSimulationSystemGroup SimulationGroup;
			
			public UTick GetTick()
			{
				return SimulationGroup.GetServerTick();
			}
		}

		public class QueryContext : ExternalContextBase
		{
			public EntityQuery Player;

			public TrainingGameMode System;

			public EntityQueryBuilder GetBuilder()
			{
				return System.Entities;
			}
		}
	}
}