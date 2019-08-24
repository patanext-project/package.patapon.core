using GmMachine;
using GmMachine.Blocks;
using Misc.GmMachine.Blocks;
using Misc.GmMachine.Contexts;
using package.patapon.core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Patapon4TLB.GameModes
{
	public partial class MpVersusHeadOnGameModeAsync
	{
		public class StartRoundBlock : BlockCollection
		{
			private VersusHeadOnContext m_HeadOnContext;
			private VersusHeadOnQueriesContext m_QueriesContext;

			public Block            SpawnUnitBlock;
			public WaitingTickBlock CounterBlock;

			public StartRoundBlock(string name) : base(name)
			{
				Add(SpawnUnitBlock = new Block("Spawn Units"));
				Add(CounterBlock   = new WaitingTickBlock("321 Counter"));
			}

			protected override bool OnRun()
			{
				if (RunNext(SpawnUnitBlock))
				{
					SpawnUnits();
					CounterBlock.SetTicksFromMs(3000);

					m_QueriesContext.GetEntityQueryBuilder().ForEach((ref RhythmEngineProcess process) => { process.StartTime = CounterBlock.Target.Ms; });
					
					return false;
				}

				if (RunNext(CounterBlock))
					return false;

				return true;
			}

			protected override void OnReset()
			{
				base.OnReset();

				m_HeadOnContext         = Context.GetExternal<VersusHeadOnContext>();
				m_QueriesContext        = Context.GetExternal<VersusHeadOnQueriesContext>();
				
				CounterBlock.TickGetter = m_HeadOnContext;
			}

			private int[] m_TeamAttackAverage;
			private int[] m_TeamHealthAverage;
			private int[] m_TeamUnitCount;

			private void SpawnUnits()
			{
				bool IsFormationValid(Entity formation, World world)
				{
					return world.EntityManager.GetComponentData<FormationTeam>(formation).TeamIndex != 0;
				}

				void OnUnitCreated(Entity formation, int formationIndex, Entity army, int armyIndex, Entity unit, World world)
				{
					var gmContext = Context.GetExternal<VersusHeadOnContext>();

					var entityMgr = world.EntityManager;
					var team      = entityMgr.GetComponentData<FormationTeam>(formation);

					entityMgr.AddComponentData(unit, new Relative<TeamDescription>(gmContext.Teams[team.TeamIndex - 1].Target));
					entityMgr.AddComponentData(unit, new GameModeUnit
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
				var queries  = Context.GetExternal<VersusHeadOnQueriesContext>();

				m_TeamAttackAverage = new int[2];
				m_TeamHealthAverage = new int[2];
				m_TeamUnitCount     = new int[2];
				Utility.CreateUnitsBase(queries.GameModeSystem, worldCtx.World, queries.Formation, IsFormationValid, _ => true, OnUnitCreated);

				var teams = Context.GetExternal<VersusHeadOnContext>().Teams;
				for (var i = 0; i < teams.Length; i++)
				{
					teams[i].AveragePower = m_TeamHealthAverage[1 - i] * m_TeamUnitCount[1 - i] - m_TeamAttackAverage[i] * m_TeamUnitCount[i];
				}
			}
		}
	}
}