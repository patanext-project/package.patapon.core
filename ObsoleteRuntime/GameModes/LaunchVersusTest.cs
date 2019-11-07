using Patapon4TLB.Core;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Revolution.NetCode;

namespace Patapon4TLB.GameModes
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class LaunchVersusTest : ComponentSystem
	{
		private bool        m_IsLaunch;
		private EntityQuery m_PlayerQuery;
		private EntityQuery m_UnitFormationQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PlayerQuery        = GetEntityQuery(typeof(GamePlayerReadyTag));
			m_UnitFormationQuery = GetEntityQuery(typeof(UnitFormation), ComponentType.Exclude<Relative<PlayerDescription>>());
			
			// Create two test formations
			const int formationCount = 2;
			for (var _ = 0; _ != formationCount; _++)
			{
				var formationRoot = EntityManager.CreateEntity(typeof(GameFormationTag), typeof(FormationTeam), typeof(FormationRoot));
				{
					const int armyCount = 4;
					for (var i = 0; i != armyCount; i++)
					{
						var armyEntity = EntityManager.CreateEntity(typeof(ArmyFormation), typeof(FormationParent), typeof(FormationChild));
						EntityManager.SetComponentData(armyEntity, new FormationParent {Value = formationRoot});

						var unitEntity = EntityManager.CreateEntity(typeof(UnitFormation), typeof(UnitStatistics), typeof(UnitDefinedAbilities), typeof(FormationParent));
						EntityManager.SetComponentData(unitEntity, new FormationParent {Value = armyEntity});
						// taterazay
						EntityManager.SetComponentData(unitEntity, new UnitStatistics
						{
							Health  = 225,
							Attack  = 24,
							Defense = 7,

							BaseWalkSpeed       = 2f,
							FeverWalkSpeed      = 2.2f,
							AttackSpeed         = 2.0f,
							MovementAttackSpeed = 2.22f,
							Weight              = 8.5f,
							AttackSeekRange     = 20f
						});
						// yarida
						/*EntityManager.SetComponentData(unitEntity, new UnitStatistics
						{
							Health  = 175,
							Attack  = 30,
							Defense = 0,

							BaseWalkSpeed       = 2f,
							FeverWalkSpeed      = 2.2f,
							AttackSpeed         = 2.0f,
							MovementAttackSpeed = 2.22f,
							Weight              = 6f,
							AttackSeekRange     = 20f
						});*/

						var definedAbilities = EntityManager.GetBuffer<UnitDefinedAbilities>(unitEntity);
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_march"), 0));
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_backward"), 0));
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_retreat"), 0));
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("basic_jump"), 0));
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_attack"), 0));
						definedAbilities.Add(new UnitDefinedAbilities(MasterServerAbilities.GetInternal("tate/basic_defense"), 0));
					}
				}

				EntityManager.SetComponentData(formationRoot, new FormationTeam {TeamIndex = _ + 1});
			}
		}

		protected override void OnUpdate()
		{
			if (m_IsLaunch)
				return;

			if (m_PlayerQuery.CalculateEntityCount() > 0)
			{
				m_IsLaunch = true;
				var mgr = World.GetOrCreateSystem<GameModeManager>();
				mgr.SetGameMode(new MpVersusHeadOn(), "VS-HeadOn");

				// Set an entity for a player
				Entities.With(m_PlayerQuery).ForEach((Entity e) =>
				{
					using (var entities = m_UnitFormationQuery.ToEntityArray(Allocator.TempJob))
					{
						Entity unit = default;
						//unit = entities[new Random((uint) Environment.TickCount).NextInt(0, entities.Length)];
						foreach (var ent in entities)
						{
							if (EntityManager.GetComponentData<FormationTeam>(EntityManager.GetComponentData<InFormation>(ent).Root).TeamIndex == 2)
							{
								unit = ent;
								break;
							}
						}

						EntityManager.AddComponentData(unit, new Relative<PlayerDescription> {Target = e});
					}
				});
			}
		}
	}
}