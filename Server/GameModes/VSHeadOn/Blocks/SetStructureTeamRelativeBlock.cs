using GmMachine;
using Misc.GmMachine.Contexts;
using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using Unity.Collections;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class SetStructureTeamRelative : Block
	{
		private MpVersusHeadOnGameMode.ModeContext    m_ModeContext;
		private MpVersusHeadOnGameMode.QueriesContext m_QueriesContext;
		private WorldContext                          m_WorldContext;

		public SetStructureTeamRelative(string name) : base(name)
		{
		}

		protected override bool OnRun()
		{
			var query = m_QueriesContext.UpdateTeam;
			if (query.CalculateEntityCount() > 0)
			{
				var entityManager   = m_WorldContext.EntityMgr;
				var entities        = query.ToEntityArray(Allocator.TempJob);
				var targetTeamArray = query.ToComponentDataArray<HeadOnTeamTarget>(Allocator.TempJob);
				entityManager.AddComponent(query, typeof(Relative<TeamDescription>));
				for (var ent = 0; ent != entities.Length; ent++)
				{
					var target = targetTeamArray[ent];
					if (target.Custom != default)
						entityManager.SetComponentData(entities[ent], new Relative<TeamDescription> {Target = target.Custom});
					else
						entityManager.SetComponentData(entities[ent], new Relative<TeamDescription> {Target = m_ModeContext.Teams[target.TeamIndex].Target});
				}

				entities.Dispose();
				targetTeamArray.Dispose();
			}

			return true;
		}

		protected override void OnReset()
		{
			base.OnReset();

			m_QueriesContext = Context.GetExternal<MpVersusHeadOnGameMode.QueriesContext>();
			m_ModeContext    = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
			m_WorldContext   = Context.GetExternal<WorldContext>();
		}
	}
}