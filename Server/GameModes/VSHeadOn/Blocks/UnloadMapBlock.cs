using GmMachine;
using Misc.GmMachine.Contexts;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class UnloadMapBlock : Block
	{
		private GameModeContext m_GameModeCtx;
		private Entity          m_RequestEntity;
		private WorldContext    m_WorldCtx;

		public UnloadMapBlock(string name) : base(name)
		{
		}

		protected override bool OnRun()
		{
			if (m_RequestEntity != default)
				return !m_GameModeCtx.IsMapLoaded;

			m_RequestEntity = m_WorldCtx.EntityMgr.CreateEntity(typeof(RequestMapUnload));
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