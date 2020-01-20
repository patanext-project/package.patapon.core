using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class EndRoundBlock : BlockCollection
	{
		public Block InitializationBlock;
		public WaitingTickBlock CaptureCounterBlock;
		public WaitingTickBlock CounterBlock;
		public Block DestroyBlock;

		public EndRoundBlock(string name) : base(name)
		{
			Add(InitializationBlock = new Block("Init"));
			Add(CaptureCounterBlock = new WaitingTickBlock("Capture counter (some delay before setting winscreen state)"));
			Add(CounterBlock = new WaitingTickBlock("Winning Screen Counter"));
			Add(DestroyBlock = new Block("Destroy and reset"));
		}

		private bool m_FirstRun;

		private MpVersusHeadOnGameMode.ModeContext    m_HeadOnModeContext;
		private MpVersusHeadOnGameMode.QueriesContext m_QueriesContext;
		private WorldContext                          m_WorldContext;

		protected override bool OnRun()
		{
			if (RunNext(InitializationBlock))
			{
				CaptureCounterBlock.SetTicksFromMs(m_HeadOnModeContext.Data.WinReason == MpVersusHeadOn.WinStatus.FlagCaptured ? 3000 : 0);
				CounterBlock.SetTicksFromMs(m_HeadOnModeContext.Data.WinReason != MpVersusHeadOn.WinStatus.Forced ? 6000 : 0);
				return false;
			}

			if (RunNext(CaptureCounterBlock))
			{
				return false;
			}

			if (RunNext(CounterBlock))
			{
				m_QueriesContext.GetEntityQueryBuilder().ForEach((ref RhythmEngineState state) => { state.IsPaused = true; });
				
				if (m_HeadOnModeContext.Data.PlayState != MpVersusHeadOn.State.OnRoundEnd)
				{
					m_HeadOnModeContext.HudSettings.PushStatus(m_HeadOnModeContext.GetTick(), default, EGameModeStatusSound.WinningSequence);
				}
				m_HeadOnModeContext.Data.PlayState = MpVersusHeadOn.State.OnRoundEnd;

				return false;
			}

			if (RunNext(DestroyBlock))
			{
				// destroy units
				m_QueriesContext.GetEntityQueryBuilder()
				                .With(m_QueriesContext.Unit)
				                .ForEach((Entity entity) => { m_WorldContext.EntityMgr.DestroyEntity(entity); });

				// reset towers
				m_QueriesContext.GetEntityQueryBuilder()
				                .ForEach((Entity entity, ref HeadOnStructure structureData) =>
				                {
					                var relative = default(Relative<TeamDescription>);
					                if (m_WorldContext.EntityMgr.TryGetComponentData(entity, out HeadOnTeamTarget target))
					                {
						                if (target.Custom != default)
							                relative = new Relative<TeamDescription> {Target = target.Custom};
						                else
							                relative = new Relative<TeamDescription> {Target = m_HeadOnModeContext.Teams[target.TeamIndex].Target};
					                }

					                if (m_WorldContext.EntityMgr.HasComponent<Relative<TeamDescription>>(entity))
						                m_WorldContext.EntityMgr.SetComponentData(entity, relative);
				                });

				if (m_HeadOnModeContext.RoundPerMatch == 0)
				{
					if (Executor is BlockAutoLoopCollection collection)
					{
						collection.Break();
					}
				}

				return true;
			}

			return false;
		}

		protected override void OnReset()
		{
			base.OnReset();

			m_HeadOnModeContext = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
			m_QueriesContext    = Context.GetExternal<MpVersusHeadOnGameMode.QueriesContext>();
			m_WorldContext      = Context.GetExternal<WorldContext>();
			
			CaptureCounterBlock.TickGetter = m_HeadOnModeContext;
			CounterBlock.TickGetter = m_HeadOnModeContext;
		}
	}
}