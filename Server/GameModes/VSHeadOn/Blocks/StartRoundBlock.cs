using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Units;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public class StartRoundBlock : BlockCollection
	{
		private MpVersusHeadOnGameMode.ModeContext    m_ModeContext;
		private MpVersusHeadOnGameMode.QueriesContext m_QueriesContext;

		public Block            CreateUnitBlock;
		public Block            SpawnUnitBlock;
		public WaitingTickBlock CounterBlock;

		public StartRoundBlock(string name) : base(name)
		{
			Add(CreateUnitBlock = new Block("Create Units"));
			Add(SpawnUnitBlock  = new Block("Spawn Units"));
			Add(CounterBlock    = new WaitingTickBlock("321 Counter"));
		}

		protected override bool OnRun()
		{
			if (RunNext(CreateUnitBlock))
			{
				CreateUnits();
				return false;
			}

			if (RunNext(SpawnUnitBlock))
			{
				SpawnUnits();
				CounterBlock.SetTicksFromMs(30);

				m_QueriesContext.GetEntityQueryBuilder().ForEach((ref FlowEngineProcess process) => { process.StartTime = CounterBlock.Target.Ms; });

				return false;
			}

			if (RunNext(CounterBlock))
				return false;

			return true;
		}

		protected override void OnReset()
		{
			base.OnReset();

			m_ModeContext    = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();
			m_QueriesContext = Context.GetExternal<MpVersusHeadOnGameMode.QueriesContext>();

			CounterBlock.TickGetter = m_ModeContext;
		}

		private int[] m_TeamAttackAverage;
		private int[] m_TeamHealthAverage;
		private int[] m_TeamUnitCount;

		private void CreateUnits()
		{
			bool IsFormationValid(Entity formation, World world)
			{
				return world.EntityManager.GetComponentData<FormationTeam>(formation).TeamIndex != 0;
			}

			void OnUnitCreated(Entity formation, int formationIndex, Entity army, int armyIndex, Entity unit, World world)
			{
				var entityMgr = world.EntityManager;
				var team      = entityMgr.GetComponentData<FormationTeam>(formation);

				entityMgr.AddComponentData(unit, new Relative<TeamDescription>(m_ModeContext.Teams[team.TeamIndex - 1].Target));
				entityMgr.AddComponentData(unit, new VersusHeadOnUnit
				{
					Team           = team.TeamIndex - 1,
					FormationIndex = formationIndex
				});

				var stat = entityMgr.GetComponentData<UnitStatistics>(unit);
				var ti   = team.TeamIndex - 1;
				if (m_TeamAttackAverage[ti] > 0)
					m_TeamAttackAverage[ti] = (int) math.lerp(m_TeamAttackAverage[ti], stat.Attack, 0.5f);
				else
					m_TeamAttackAverage[ti] = stat.Attack;
				if (m_TeamHealthAverage[ti] > 0)
					m_TeamHealthAverage[ti]  = (int) math.lerp(m_TeamHealthAverage[ti], stat.Health, 0.5f);
				else m_TeamAttackAverage[ti] = stat.Health;

				var healthEvent = entityMgr.CreateEntity(typeof(ModifyHealthEvent));
				entityMgr.SetComponentData(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, unit));

				m_TeamUnitCount[ti]++;
			}

			var worldCtx = Context.GetExternal<WorldContext>();

			m_TeamAttackAverage = new int[2];
			m_TeamHealthAverage = new int[2];
			m_TeamUnitCount     = new int[2];
			Utility.CreateUnitsBase(m_QueriesContext.GameModeSystem, worldCtx.World, m_QueriesContext.Formation, IsFormationValid, _ => true, OnUnitCreated);

			var teams = m_ModeContext.Teams;
			for (var i = 0; i < teams.Length; i++)
			{
				teams[i].AveragePower = m_TeamHealthAverage[1 - i] * m_TeamUnitCount[1 - i] - m_TeamAttackAverage[i] * m_TeamUnitCount[i];
			}
		}

		private void SpawnUnits()
		{
			var queries = Context.GetExternal<MpVersusHeadOnGameMode.QueriesContext>();
			queries.GetEntityQueryBuilder().With(queries.Unit).ForEach(SpawnUnit);
		}

		private void SpawnUnit(Entity unit)
		{
			var entityMgr = Context.GetExternal<WorldContext>().EntityMgr;
			var gmCtx     = Context.GetExternal<MpVersusHeadOnGameMode.ModeContext>();

			var gmData = entityMgr.GetComponentData<VersusHeadOnUnit>(unit);

			var team = gmCtx.Teams[gmData.Team];
			if (team.SpawnPoint != default)
			{
				var spawnPointPos = entityMgr.GetComponentData<LocalToWorld>(team.SpawnPoint).Position;
				entityMgr.SetComponentData(unit, new Translation
				{
					Value = new float3(spawnPointPos.x, 0, 0)
				});
			}
		}
	}
}