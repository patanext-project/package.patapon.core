using System;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Random = Unity.Mathematics.Random;

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

						var unitEntity = EntityManager.CreateEntity(typeof(UnitFormation), typeof(UnitStatistics), typeof(FormationParent));
						EntityManager.SetComponentData(unitEntity, new FormationParent {Value = armyEntity});
						EntityManager.SetComponentData(unitEntity, new UnitStatistics
						{
							Health              = 100,
							BaseWalkSpeed       = 2f,
							FeverWalkSpeed      = 2.2f,
							AttackSpeed         = 1.7f,
							MovementAttackSpeed = 2.22f,
							Weight              = 8f
						});
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
						var unit = entities[new Random((uint) Environment.TickCount).NextInt(0, entities.Length)];
						EntityManager.AddComponentData(unit, new Relative<PlayerDescription> {Target = e});
					}
				});
			}
		}
	}
}