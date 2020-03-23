using System.Collections.Generic;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.Training;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Units;
using Patapon.Mixed.Units.Statistics;
using Patapon.Server.GameModes.VSHeadOn;
using Patapon4TLB.Core;
using Revolution;
using Rpc;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using StormiumTeam.GameBase.EcsComponents;
using StormiumTeam.GameBase.External;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;

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
			private WorldContext    m_WorldCtx;
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

				m_WorldCtx    = Context.GetExternal<WorldContext>();
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
			private QueryBuilderContext m_QueryBuilderCtx;
			private QueryContext m_Queries;
			private WorldContext m_WorldCtx;
			private ModeContext  m_GmContext;

			protected override bool OnRun()
			{
				var entityMgr    = m_WorldCtx.EntityMgr;
				var unitProvider = m_WorldCtx.GetExistingSystem<UnitProvider>();
				var teamProvider = m_WorldCtx.GetExistingSystem<GameModeTeamProvider>();
				var clubProvider = m_WorldCtx.GetExistingSystem<ClubProvider>();
				
				m_GmContext.Teams = new TeamData[2];
				for (var t = 0; t != 2; t++)
				{
					ref var team = ref m_GmContext.Teams[t];
					team.Target = teamProvider.SpawnLocalEntityWithArguments(new GameModeTeamProvider.Create());

					// Add club...
					var club = clubProvider.SpawnLocalEntityWithArguments(new ClubProvider.Create
					{
						name           = new NativeString64(t == 0 ? "Blue" : "Red"),
						primaryColor   = t == 0 ? new Color(0.75f, 0.8f, 0.6f) : new Color(0.87f, 0.3f, 0.48f),
						secondaryColor = Color.Lerp(Color.Lerp(t == 0 ? Color.blue : Color.red, Color.white, 0.15f), Color.black, 0.15f)
					});
					entityMgr.AddComponentData(team.Target, new Relative<ClubDescription> {Target = club});
					entityMgr.AddComponent(club, typeof(GhostEntity));

					entityMgr.SetOrAddComponentData(team.Target, new UnitDirection {Value = (sbyte) (t == 0 ? 1 : -1)});
				}

				// -- Set enemies of each team
				for (var t = 0; t != 2; t++)
				{
					var enemies = entityMgr.GetBuffer<TeamEnemies>(m_GmContext.Teams[t].Target);
					enemies.Add(new TeamEnemies {Target = m_GmContext.Teams[1 - t].Target});
				}

				// ----------------------------- //
				// Set team of players
				// Create player rhythm engines
				// Create player unit targets
				// Create player unit
				m_QueryBuilderCtx.From.With(m_Queries.Player).ForEach(player =>
				{
					// Player without NetworkOwner mean that it's a bot.
					if (entityMgr.TryGetComponentData(player, out NetworkOwner networkOwner))
					{
						var rhythmEngineProvider = m_WorldCtx.GetOrCreateSystem<RhythmEngineProvider>();
						var rhythmEnt = rhythmEngineProvider.SpawnLocalEntityWithArguments(new RhythmEngineProvider.Create
						{
							UseClientSimulation = true
						});

						entityMgr.SetOrAddComponentData(rhythmEnt, networkOwner);
						entityMgr.SetOrAddComponentData(rhythmEnt, new FlowEngineProcess {StartTime = m_GmContext.GetTick().Ms});
						entityMgr.AddComponent(rhythmEnt, typeof(GhostEntity));
						entityMgr.AddComponentData(rhythmEnt, new OwnerServerId {Value = m_WorldCtx.EntityMgr.GetComponentData<GamePlayer>(player).ServerId});

						entityMgr.ReplaceOwnerData(rhythmEnt, player);
					}

					var unitTarget = entityMgr.CreateEntity(typeof(UnitTargetDescription), typeof(Translation), typeof(LocalToWorld), typeof(Relative<PlayerDescription>));
					entityMgr.AddComponent(unitTarget, typeof(GhostEntity));
					entityMgr.AddComponentData(unitTarget, EntityDescription.New<UnitTargetDescription>());
					entityMgr.ReplaceOwnerData(unitTarget, player);

					// Create unit
					var capsuleColl = CapsuleCollider.Create(new CapsuleGeometry
					{
						Radius  = 0.5f,
						Vertex0 = 0,
						Vertex1 = math.up() * 1.6f
					});

					var spawnedUnit = unitProvider.SpawnLocalEntityWithArguments(new UnitProvider.Create
					{
						Direction       = UnitDirection.Right,
						MovableCollider = capsuleColl,
						Mass            = PhysicsMass.CreateKinematic(capsuleColl.Value.MassProperties),
						Settings        = new UnitStatistics()
					});

					var displayEquipment = new UnitDisplayedEquipment();
					var targetKit        = UnitKnownTypes.Pingrek;
					var statistics       = default(UnitStatistics);
					var definedAbilities = entityMgr.AddBuffer<UnitDefinedAbilities>(spawnedUnit);
					KitTempUtility.Set(targetKit, ref statistics, definedAbilities, ref displayEquipment);

					entityMgr.SetOrAddComponentData(spawnedUnit, new UnitCurrentKit {Value = targetKit});
					entityMgr.SetOrAddComponentData(spawnedUnit, statistics);
					entityMgr.SetOrAddComponentData(spawnedUnit, displayEquipment);

					entityMgr.AddComponent(spawnedUnit, typeof(GhostEntity));
					entityMgr.ReplaceOwnerData(spawnedUnit, player);

					var childrenBuffer = entityMgr.GetBuffer<OwnerChild>(player).ToNativeArray(Allocator.Temp);
					for (var i = 0; i != childrenBuffer.Length; i++)
					{
						if (entityMgr.HasComponent(childrenBuffer[i].Child, typeof(RhythmEngineDescription)))
							entityMgr.SetOrAddComponentData(spawnedUnit, new Relative<RhythmEngineDescription>(childrenBuffer[i].Child));
						if (entityMgr.HasComponent(childrenBuffer[i].Child, typeof(UnitTargetDescription)))
							entityMgr.SetOrAddComponentData(spawnedUnit, new Relative<UnitTargetDescription>(childrenBuffer[i].Child));
					}

					childrenBuffer.Dispose();

					entityMgr.AddComponent(spawnedUnit, typeof(UnitTargetControlTag));

					var healthEntity = m_WorldCtx.GetExistingSystem<DefaultHealthData.InstanceProvider>().SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance
					{
						max   = statistics.Health,
						value = statistics.Health,
						owner = spawnedUnit
					});
					entityMgr.AddComponent(healthEntity, typeof(GhostEntity));
					MasterServerAbilities.Convert(m_GmContext.SimulationGroup, spawnedUnit, entityMgr.GetBuffer<UnitDefinedAbilities>(spawnedUnit));

					entityMgr.SetOrAddComponentData(spawnedUnit, new Relative<TeamDescription>(m_GmContext.Teams[0].Target));
					entityMgr.SetOrAddComponentData(unitTarget, new Relative<TeamDescription>(m_GmContext.Teams[0].Target));

					entityMgr.SetComponentData(player, new ServerCameraState
					{
						Data =
						{
							Target = spawnedUnit
						}
					});

					m_GmContext.CurrentUnit = spawnedUnit;
				});

				// Spawn dummy
				SpawnDummy(10, 1);
				SpawnDummy(-2, 0);

				return true;
			}

			private void SpawnDummy(float position, int team)
			{
				var entityMgr    = m_WorldCtx.EntityMgr;
				var unitProvider = m_WorldCtx.GetExistingSystem<UnitProvider>();

				// Create unit
				var capsuleColl = CapsuleCollider.Create(new CapsuleGeometry
				{
					Radius  = 0.5f,
					Vertex0 = 0,
					Vertex1 = math.up() * 1.6f
				});

				var spawnedUnit = unitProvider.SpawnLocalEntityWithArguments(new UnitProvider.Create
				{
					Direction       = team == 0 ? UnitDirection.Right : UnitDirection.Left,
					MovableCollider = capsuleColl,
					Mass            = PhysicsMass.CreateKinematic(capsuleColl.Value.MassProperties),
					Settings        = new UnitStatistics()
				});

				entityMgr.SetComponentData(spawnedUnit, new Translation {Value = {x = position}});

				var displayEquipment = new UnitDisplayedEquipment();
				var targetKit        = new NativeString64("training_room_dummy");
				var statistics       = default(UnitStatistics);
				var definedAbilities = entityMgr.AddBuffer<UnitDefinedAbilities>(spawnedUnit);
				definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal(P4OfficialAbilities.BasicMarch), 0));

				entityMgr.SetOrAddComponentData(spawnedUnit, new UnitCurrentKit {Value = targetKit});
				entityMgr.SetOrAddComponentData(spawnedUnit, statistics);
				entityMgr.SetOrAddComponentData(spawnedUnit, displayEquipment);

				entityMgr.AddComponent(spawnedUnit, typeof(GhostEntity));
				entityMgr.SetComponentData(spawnedUnit, new LivableHealth {IsDead = false, Value = 1, Max = 1});

				entityMgr.AddComponent(spawnedUnit, typeof(UnitTargetControlTag));
				MasterServerAbilities.Convert(m_GmContext.SimulationGroup, spawnedUnit, entityMgr.GetBuffer<UnitDefinedAbilities>(spawnedUnit));

				entityMgr.SetOrAddComponentData(spawnedUnit, new Relative<TeamDescription>(m_GmContext.Teams[team].Target));
			}

			protected override void OnReset()
			{
				base.OnReset();

				m_QueryBuilderCtx = Context.GetExternal<QueryBuilderContext>();
				m_Queries   = Context.GetExternal<QueryContext>();
				m_WorldCtx  = Context.GetExternal<WorldContext>();
				m_GmContext = Context.GetExternal<ModeContext>();
			}
		}

		public class PlayLoopBlock : Block
		{
			private QueryBuilderContext m_QueryBuilderCtx;
			private WorldContext m_WorldCtx;
			private ModeContext  m_GmContext;

			protected override bool OnRun()
			{
				m_WorldCtx.GetExistingSystem<GameEventRuleSystemGroup>().Process();
				
				m_QueryBuilderCtx.From.ForEach((Entity ent, ref TrainingRoomSetKit rpc) =>
				{
					var targetKit = default(NativeString64);
					switch (rpc.KitId)
					{
						case 0:
							targetKit = UnitKnownTypes.Taterazay;
							break;
						case 1:
							targetKit = UnitKnownTypes.Yarida;
							break;
						case 2:
							targetKit = UnitKnownTypes.Pingrek;
							break;
						case 3:
							targetKit = UnitKnownTypes.Kibadda;
							break;
					}

					if (targetKit.Equals(default))
						return;

					var entityMgr        = m_WorldCtx.EntityMgr;
					var displayEquipment = new UnitDisplayedEquipment();
					var statistics       = default(UnitStatistics);

					var children = entityMgr.GetBuffer<OwnerChild>(m_GmContext.CurrentUnit).ToNativeArray(Allocator.Temp);
					foreach (var oc in children)
					{
						if (!entityMgr.HasComponent(oc.Child, typeof(ActionDescription)))
							continue;

						entityMgr.DestroyEntity(oc.Child);
					}

					var definedAbilities = entityMgr.GetBuffer<UnitDefinedAbilities>(m_GmContext.CurrentUnit);
					definedAbilities.Clear();
					KitTempUtility.Set(targetKit, ref statistics, definedAbilities, ref displayEquipment);

					entityMgr.SetComponentData(m_GmContext.CurrentUnit, displayEquipment);
					entityMgr.SetComponentData(m_GmContext.CurrentUnit, new UnitCurrentKit {Value = targetKit});

					MasterServerAbilities.Convert(m_GmContext.SimulationGroup, m_GmContext.CurrentUnit, entityMgr.GetBuffer<UnitDefinedAbilities>(m_GmContext.CurrentUnit));

					if (!entityMgr.TryGetComponentData<OwnerActiveAbility>(m_GmContext.CurrentUnit, out var active))
						return;

					active.Active                = default;
					active.Incoming              = default;
					active.CurrentCombo          = default;
					active.LastActivationTime    = default;
					active.LastCommandActiveTime = default;

					entityMgr.SetComponentData(m_GmContext.CurrentUnit, active);
					entityMgr.DestroyEntity(ent);
				});

				return true;
			}

			protected override void OnReset()
			{
				base.OnReset();

				m_QueryBuilderCtx = Context.GetExternal<QueryBuilderContext>();
				m_WorldCtx        = Context.GetExternal<WorldContext>();
				m_GmContext       = Context.GetExternal<ModeContext>();
			}
		}

		public struct TeamData
		{
			public Entity Target;
		}

		public class ModeContext : ExternalContextBase, ITickGetter
		{
			public ServerSimulationSystemGroup SimulationGroup;
			public TeamData[]                  Teams;
			public Entity                      CurrentUnit;

			public UTick GetTick()
			{
				return SimulationGroup.GetServerTick();
			}
		}

		public class QueryContext : ExternalContextBase
		{
			public EntityQuery Player;
			public TrainingGameMode System;
		}
	}
}