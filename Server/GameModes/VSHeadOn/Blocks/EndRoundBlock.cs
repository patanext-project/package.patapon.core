using GmMachine;
using Misc.GmMachine.Contexts;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class EndRoundBlock : Block
	{
		public EndRoundBlock(string name) : base(name) {}

		private bool m_FirstRun;
		
		private MpVersusHeadOnGameMode.ModeContext m_HeadOnModeContext;
		private MpVersusHeadOnGameMode.QueriesContext m_QueriesContext;
		private WorldContext          m_WorldContext;

		protected override bool OnRun()
		{
			m_QueriesContext.GetEntityQueryBuilder()
			                .With(m_QueriesContext.Unit)
			                .ForEach((Entity entity) => { m_WorldContext.EntityMgr.DestroyEntity(entity); });

			return true;
		}

		protected override void OnReset()
		{
			base.OnReset();
			
			m_HeadOnModeContext = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
			m_QueriesContext = Context.GetExternal<MpVersusHeadOnGameMode.QueriesContext>();
			m_WorldContext = Context.GetExternal<WorldContext>();
		}
	}
}