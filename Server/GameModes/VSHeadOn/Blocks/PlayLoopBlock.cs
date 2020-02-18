using GameModes.VSHeadOn;
using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using StormiumTeam.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class PlayLoopBlock : Block
	{
		private GameEventRuleSystemGroup              m_GameEventRuleSystemGroup;
		private MpVersusHeadOnGameMode.ModeContext    m_HeadOnModeContext;
		private MpVersusHeadOnGameMode.QueriesContext m_Queries;

		private VersusHeadOnRuleGroup m_VersusHeadOnRuleGroup;
		private WorldContext          m_WorldContext;

		private float m_CheckLeadDelay;
		private int m_TeamInLead;

		private bool m_IsOverTime;
		private bool m_OneMinuteMark;
		
		public PlayLoopBlock(string name) : base(name)
		{
		}

		protected override bool OnRun()
		{
			var entityMgr = m_WorldContext.EntityMgr;
			
			if (Input.GetKeyDown(KeyCode.H))
			{
				m_Queries.GetEntityQueryBuilder().With(m_Queries.Unit).ForEach(e =>
				{
					var ev = entityMgr.CreateEntity(typeof(GameEvent), typeof(TargetDamageEvent), typeof(Translation), typeof(GhostEntity));
					entityMgr.SetComponentData(ev, new TargetDamageEvent
					{
						Destination = e,
						Damage      = -25
					});
					entityMgr.SetComponentData(ev, new Translation {Value = entityMgr.GetComponentData<Translation>(e).Value + new float3(0, 1, 0)});
				});
			}
			
			m_GameEventRuleSystemGroup.Process();
			m_VersusHeadOnRuleGroup.Process();

			ref var gameModeData = ref m_HeadOnModeContext.Data;
			ref var hud = ref m_HeadOnModeContext.HudSettings;
			
			// ----------------------------- //
			// Check leader
			m_CheckLeadDelay -= m_HeadOnModeContext.GetTick().Delta;
			if (m_CheckLeadDelay <= 0)
			{
				for (var i = 0; i != 2; i++)
				{
					if (gameModeData.GetPoints(i) > gameModeData.GetPoints(1 - i)
					    && m_TeamInLead != i)
					{
						m_TeamInLead = i;
						hud.PushStatus(m_HeadOnModeContext.GetTick(), "comeback_upset", m_TeamInLead.ToString(), EGameModeStatusSound.NewLeader);
					}
				}
			}

			// ----------------------------- //
			// Elimination events
			var eliminationEvents = m_HeadOnModeContext.UnitEliminationEvents;
			for (int i = 0, length = eliminationEvents.Length; i < length; i++)
			{
				var ev = eliminationEvents[i];
				if (ev.InstigatorTeam >= 0 && ev.InstigatorTeam <= 1)
				{
					ref var points = ref gameModeData.GetPoints(ev.InstigatorTeam);
					points += 25;

					if (gameModeData.GetEliminations(0) == 0 && gameModeData.GetEliminations(1) == 0)
					{
						var sound = EGameModeStatusSound.None;
						if (hud.StatusSound == EGameModeStatusSound.NewLeader)
							sound = EGameModeStatusSound.NewLeader;
						hud.PushStatus(m_HeadOnModeContext.GetTick(), "First Takedown!", sound);
					}

					ref var eliminations = ref gameModeData.GetEliminations(ev.InstigatorTeam);
					eliminations++;
				}
			}

			// -- clear elimination events
			eliminationEvents.Clear();

			// ----------------------------- //
			// Destroy events
			var destroyEvents = m_HeadOnModeContext.DestroyAreaEvents;
			for (int i = 0, length = destroyEvents.Length; i < length; i++)
			{
				var ev             = destroyEvents[i];
				var instigatorTeam = -1;
				var structureTeam  = -1;
				for (var t = 0; t != m_HeadOnModeContext.Teams.Length; t++)
				{
					if (ev.EntityTeam == m_HeadOnModeContext.Teams[t].Target)
						structureTeam = t;
					if (ev.InstigatorTeam == m_HeadOnModeContext.Teams[t].Target)
						instigatorTeam = t;
				}

				if (instigatorTeam >= 0 && instigatorTeam <= 1)
				{
					ref var points = ref gameModeData.GetPoints(instigatorTeam);
					points += ev.Score;
				}

				// Destroy cannons
				var children = entityMgr.GetBuffer<OwnerChild>(ev.Entity).ToNativeArray(Allocator.Temp);
				foreach (var oc in children)
				{
					if (!entityMgr.TryGetComponentData(oc.Child, out HeadOnCannon cannon))
						continue;

					cannon.Active        = false;

					var healthEvent = entityMgr.CreateEntity(typeof(ModifyHealthEvent));
					entityMgr.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetNone, 0, oc.Child));
					entityMgr.SetOrAddComponentData(oc.Child, new LivableHealth
					{
						IsDead = true
					});

					entityMgr.SetComponentData(oc.Child, cannon);
				}
			}

			// -- clear destroy events
			destroyEvents.Clear();

			// ----------------------------- //
			// Respawn events
			var respawnEvents = m_HeadOnModeContext.RespawnEvents;
			for (int i = 0, length = respawnEvents.Length; i < length; i++)
			{
				var unit   = respawnEvents[i];
				var gmUnit = entityMgr.GetComponentData<VersusHeadOnUnit>(unit);
				Utility.RespawnUnit(entityMgr, unit, entityMgr.GetComponentData<LocalToWorld>(m_HeadOnModeContext.Teams[gmUnit.Team].SpawnPoint).Position);

				var healthEvent = entityMgr.CreateEntity(typeof(ModifyHealthEvent));
				entityMgr.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, unit));
			}

			// -- clear respawn events
			respawnEvents.Clear();

			// ----------------------------- //
			// Capture events (from towers and walls)
			var captureEvents = m_HeadOnModeContext.CaptureEvents;
			for (int i = 0, length = captureEvents.Length; i < length; i++)
			{
				var relativeTeam = entityMgr.GetComponentData<Relative<TeamDescription>>(captureEvents[i].Source).Target;
				var structure    = entityMgr.GetComponentData<HeadOnStructure>(captureEvents[i].Source);
				var team         = relativeTeam == gameModeData.Team0 ? 0 : 1;

				ref var points = ref gameModeData.GetPoints(team);
				points += structure.ScoreType == HeadOnStructure.EScoreType.TowerControl ? 50 :
					structure.ScoreType == HeadOnStructure.EScoreType.Tower              ? 25 :
					                                                                       10;

				// Get structure and set health...
				var value = m_HeadOnModeContext.Teams[team].AveragePower;
				var healthContainer = entityMgr.GetBuffer<HealthContainer>(captureEvents[i].Source);
				for (var h = 0; h != healthContainer.Length; h++)
				{
					var target = healthContainer[h].Target;
					if (!entityMgr.HasComponent<DefaultHealthData>(target))
						continue;

					entityMgr.SetComponentData(target, new DefaultHealthData
					{
						Value = (int) (math.max(value, 1) * structure.HealthModifier),
						Max   = (int) (math.max(value, 1) * structure.HealthModifier)
					});
				}

				var healthEvent = entityMgr.CreateEntity(typeof(ModifyHealthEvent));
				entityMgr.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, captureEvents[i].Source));
				entityMgr.SetComponentData(captureEvents[i].Source, new LivableHealth
				{
					IsDead = false
				});

				var children = entityMgr.GetBuffer<OwnerChild>(captureEvents[i].Source).ToNativeArray(Allocator.Temp);
				foreach (var oc in children)
				{
					if (!entityMgr.TryGetComponentData(oc.Child, out HeadOnCannon cannon))
						continue;

					cannon.Active        = true;
					cannon.NextShootTick = UTick.AddMs(m_HeadOnModeContext.GetTick(), 3000);

					healthEvent = entityMgr.CreateEntity(typeof(ModifyHealthEvent));
					entityMgr.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, oc.Child));
					
					healthContainer = entityMgr.GetBuffer<HealthContainer>(oc.Child);
					for (var h = 0; h != healthContainer.Length; h++)
					{
						var target = healthContainer[h].Target;
						if (!entityMgr.HasComponent<DefaultHealthData>(target))
							continue;

						entityMgr.SetComponentData(target, new DefaultHealthData
						{
							Value = (int) (math.max(value, 1) * cannon.HealthModifier),
							Max   = (int) (math.max(value, 1) * cannon.HealthModifier)
						});
					}

					entityMgr.SetComponentData(oc.Child, new LivableHealth
					{
						IsDead = false,
						Value = 1,
						Max = 1
					});
					entityMgr.SetComponentData(oc.Child, cannon);
				}

				if (structure.ScoreType == HeadOnStructure.EScoreType.TowerControl)
					hud.PushStatus(m_HeadOnModeContext.GetTick(), "Control Tower Captured!", EGameModeStatusSound.TowerControlCaptured);
				m_CheckLeadDelay = 1;
			}

			// -- clear capture events
			captureEvents.Clear();

			// ----------------------------- //
			// Set winning team
			var winningTeam = -1;

			// Check if an unit touched the enemy flag
			using (var entities = m_Queries.Unit.ToEntityArray(Allocator.TempJob))
			using (var translations = m_Queries.Unit.ToComponentDataArray<Translation>(Allocator.TempJob))
			using (var gmUnitArray = m_Queries.Unit.ToComponentDataArray<VersusHeadOnUnit>(Allocator.TempJob))
			{
				var teams = m_HeadOnModeContext.Teams;
				for (var i = 0; i != entities.Length; i++)
				{
					var oppositeTeam   = teams[1 - gmUnitArray[i].Team];
					var oppositeFlagTr = m_WorldContext.EntityMgr.GetComponentData<Translation>(oppositeTeam.Flag).Value;
					var side           = gmUnitArray[i].Team == 0 ? -1 : 1;
					if ((oppositeFlagTr.x - translations[i].Value.x) * side >= 0)
					{
						winningTeam                        = gmUnitArray[i].Team;
						m_HeadOnModeContext.Data.WinReason = MpVersusHeadOn.WinStatus.FlagCaptured;

						if (m_WorldContext.EntityMgr.TryGetComponentData(oppositeTeam.Target, out Relative<ClubDescription> clubDesc))
						{
							hud.PushStatus(m_HeadOnModeContext.GetTick(), TL._("HeadOn", "{0} flag captured!"));
							hud.StatusMessageArg0 = m_WorldContext.EntityMgr.GetComponentData<ClubInformation>(clubDesc.Target).Name;
							hud.StatusSound       = EGameModeStatusSound.FlagCaptured;
						}
						else
						{
							hud.PushStatus(m_HeadOnModeContext.GetTick(), "Flag Captured!", EGameModeStatusSound.FlagCaptured);
						}
					}
				}
			}

			// If no team are currently winning, check if the time has been ended
			if (winningTeam < 0 && m_HeadOnModeContext.GetTick().Ms > m_HeadOnModeContext.Data.EndTime)
			{
				for (var i = 0; i != m_HeadOnModeContext.Teams.Length && winningTeam < 0; i++)
				{
					if (gameModeData.GetPointReadOnly(i) > gameModeData.GetPoints(1 - i))
					{
						winningTeam = i;
						m_HeadOnModeContext.Data.WinReason = MpVersusHeadOn.WinStatus.MorePoints;
						if (m_WorldContext.EntityMgr.TryGetComponentData(m_HeadOnModeContext.Teams[i].Target, out Relative<ClubDescription> clubDesc))
						{
							hud.PushStatus(m_HeadOnModeContext.GetTick(), TL._("HeadOn", "Team {0} wins!"), m_WorldContext.EntityMgr.GetComponentData<ClubInformation>(clubDesc.Target).Name);
						}
					}
				}
			}
			
			if (winningTeam < 0 && m_HeadOnModeContext.GetTick().Ms > m_HeadOnModeContext.Data.EndTime && !m_IsOverTime)
			{
				m_IsOverTime = true;
				hud.PushStatus(m_HeadOnModeContext.GetTick(), "Overtime!");
			}

			if (!m_OneMinuteMark && (gameModeData.EndTime - m_HeadOnModeContext.GetTick().Ms) < 60 * 1000)
			{
				m_OneMinuteMark = true;
				hud.PushStatus(m_HeadOnModeContext.GetTick(), "1 minute left!");
			}
			
			// DEBUG START
			if (Input.GetKeyDown(KeyCode.F))
			{
				m_HeadOnModeContext.Data.WinReason = MpVersusHeadOn.WinStatus.FlagCaptured;
				hud.PushStatus(m_HeadOnModeContext.GetTick(), TL._("HeadOn", "{0} flag captured!"), "Blue");
				//winningTeam                        = 0;
			}

			if (Input.GetKeyDown(KeyCode.R))
			{
				m_HeadOnModeContext.Data.WinReason = MpVersusHeadOn.WinStatus.Forced;
				winningTeam = 0;
			}
			// DEBUG END

			if (Input.GetKeyDown(KeyCode.L))
			{
				m_Queries.GetEntityQueryBuilder().With(m_Queries.Unit).ForEach(e =>
				{
					entityMgr.SetComponentData(entityMgr.CreateEntity(typeof(ModifyHealthEvent)), new ModifyHealthEvent
					{
						Type = ModifyHealthType.SetNone,
						Target = e
					});
				});
			}

			m_Queries.GetEntityQueryBuilder().ForEach((Entity reqEnt, ref HeadOnSpectateRpc spectateCmd, ref ReceiveRpcCommandRequestComponent receive) =>
			{
				if (entityMgr.Exists(receive.SourceConnection))
				{
					var commandTarget = entityMgr.GetComponentData<CommandTargetComponent>(receive.SourceConnection);
					if (commandTarget.targetEntity == default || !entityMgr.HasComponent<OwnerChild>(commandTarget.targetEntity))
						return;

					var children = entityMgr.GetBuffer<OwnerChild>(commandTarget.targetEntity).ToNativeArray(Allocator.Temp);
					foreach (var oc in children)
					{
						if (entityMgr.HasComponent<RhythmEngineDescription>(oc.Child)
						    || entityMgr.HasComponent<UnitDescription>(oc.Child)
						    || entityMgr.HasComponent<UnitTargetDescription>(oc.Child))
							entityMgr.DestroyEntity(oc.Child);
					}

					if (entityMgr.HasComponent<HeadOnPlaying>(commandTarget.targetEntity))
						entityMgr.RemoveComponent<HeadOnPlaying>(commandTarget.targetEntity);

				}

				entityMgr.DestroyEntity(reqEnt);
			});
			
			m_Queries.GetEntityQueryBuilder().WithNone<HeadOnPlaying, HeadOnSpectating>().ForEach((Entity e, ref GamePlayer gp) =>
			{
				entityMgr.AddComponent(e, typeof(HeadOnSpectating));
				entityMgr.SetComponentData(e, new ServerCameraState
				{
					Data =
					{
						Target = e,
						Mode   = CameraMode.Forced
					}
				});
			});
			
			// -- End this.
			if (winningTeam >= 0 && Executor is BlockAutoLoopCollection collection)
			{
				gameModeData.WinningTeam = winningTeam;
				collection.Break();
			}

			return false;
		}

		protected override void OnReset()
		{
			base.OnReset();

			m_TeamInLead   = -1;
			m_IsOverTime = false;
			m_OneMinuteMark = false;
			m_WorldContext = Context.GetExternal<WorldContext>();
			{
				m_VersusHeadOnRuleGroup    = m_WorldContext.GetOrCreateSystem<VersusHeadOnRuleGroup>();
				m_GameEventRuleSystemGroup = m_WorldContext.GetOrCreateSystem<GameEventRuleSystemGroup>();
			}
			m_HeadOnModeContext = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
			m_Queries           = Context.GetExternal<MpVersusHeadOnGameMode.QueriesContext>();
		}
	}
}