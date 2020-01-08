using GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class PlayLoopBlock : BlockCollection
	{
		private GameEventRuleSystemGroup           m_GameEventRuleSystemGroup;
		private MpVersusHeadOnGameMode.ModeContext m_HeadOnModeContext;

		private VersusHeadOnRuleGroup m_VersusHeadOnRuleGroup;
		private WorldContext          m_WorldContext;

		public PlayLoopBlock(string name) : base(name)
		{
		}

		protected override bool OnRun()
		{
			m_GameEventRuleSystemGroup.Process();
			m_VersusHeadOnRuleGroup.Process();

			var entityMgr = m_WorldContext.EntityMgr;

			ref var gameModeData = ref m_HeadOnModeContext.Data;

			// ----------------------------- //
			// Elimination events
			var eliminationEvents = m_HeadOnModeContext.EliminationEvents;
			for (int i = 0, length = eliminationEvents.Length; i < length; i++)
			{
				var ev = eliminationEvents[i];
				if (ev.InstigatorTeam >= 0 && ev.InstigatorTeam <= 1)
				{
					ref var points = ref gameModeData.GetPoints(ev.InstigatorTeam);
					points += 25;

					ref var eliminations = ref gameModeData.GetEliminations(ev.InstigatorTeam);
					eliminations++;
				}
			}

			// -- clear elimination events
			eliminationEvents.Clear();
			
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
				points += structure.Type == HeadOnStructure.EType.TowerControl ? 50 :
					structure.Type == HeadOnStructure.EType.Tower              ? 25 :
					                                                             10;

				// Get structure and set health...
				var healthContainer = entityMgr.GetBuffer<HealthContainer>(captureEvents[i].Source);
				for (var h = 0; h != healthContainer.Length; h++)
				{
					var target = healthContainer[i].Target;
					if (!entityMgr.HasComponent<DefaultHealthData>(target))
						continue;

					var value = m_HeadOnModeContext.Teams[team].AveragePower;
					entityMgr.SetComponentData(target, new DefaultHealthData
					{
						Value = value,
						Max   = value
					});
				}

				var healthEvent = entityMgr.CreateEntity(typeof(ModifyHealthEvent));
				entityMgr.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, captureEvents[i].Source));
				entityMgr.SetComponentData(captureEvents[i].Source, new LivableHealth
				{
					IsDead = false
				});
			}

			// -- clear capture events
			captureEvents.Clear();

			// press U to finish the loop :)
			// todo: there should be a real way to finish this xd
			if (Input.GetKeyDown(KeyCode.U) && Executor is BlockAutoLoopCollection collection)
			{
				collection.Break();
			}

			return true;
		}

		protected override void OnReset()
		{
			base.OnReset();

			m_WorldContext = Context.GetExternal<WorldContext>();
			{
				m_VersusHeadOnRuleGroup    = m_WorldContext.GetOrCreateSystem<VersusHeadOnRuleGroup>();
				m_GameEventRuleSystemGroup = m_WorldContext.GetOrCreateSystem<GameEventRuleSystemGroup>();
			}
			m_HeadOnModeContext = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
		}
	}
}