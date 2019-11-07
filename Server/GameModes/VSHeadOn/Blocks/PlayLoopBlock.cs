using GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class PlayLoopBlock : BlockCollection
	{
		private WorldContext                           m_WorldContext;
		private MpVersusHeadOnGameMode.ModeContext m_HeadOnModeContext;

		private VersusHeadOnRuleGroup    m_VersusHeadOnRuleGroup;
		private GameEventRuleSystemGroup m_GameEventRuleSystemGroup;

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

			return false;
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