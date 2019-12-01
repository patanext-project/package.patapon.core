using System;
using Patapon.Mixed.Units;
using Patapon4TLB.Core;
using Patapon4TLB.Default;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public static class Utility
	{
		public static void CreateUnitsBase(ComponentSystemBase       dummySystem,
		                                   World                     worldOrigin,      EntityQuery        formationQuery,
		                                   Func<Entity, World, bool> isFormationValid, Func<Entity, bool> isArmyValid, Action<Entity, int, Entity, int, Entity, World> onEntityCreated)
		{
			var unitProvider = worldOrigin.GetExistingSystem<UnitProvider>();

			var entityMgr             = worldOrigin.EntityManager;
			var wasFormationQueryNull = formationQuery == null;
			if (wasFormationQueryNull)
			{
				formationQuery = entityMgr.CreateEntityQuery(typeof(GameFormationTag), typeof(FormationRoot));
			}

			using (var entities = formationQuery.ToEntityArray(Allocator.TempJob))
			{
				for (var form = 0; form != entities.Length; form++)
				{
					var team = entityMgr.GetComponentData<FormationTeam>(entities[form]);
					if (!isFormationValid(entities[form], worldOrigin))
						continue;

					var armies = entityMgr.GetBuffer<FormationChild>(entities[form]).ToNativeArray(Allocator.TempJob);
					for (var arm = 0; arm != armies.Length; arm++)
					{
						if (!isArmyValid(armies[arm].Value))
							continue;

						var units = entityMgr.GetBuffer<FormationChild>(armies[arm].Value).ToNativeArray(Allocator.TempJob);
						for (var unt = 0; unt != units.Length; unt++)
						{
							var capsuleColl = Unity.Physics.CapsuleCollider.Create(0, math.up() * 2, 0.5f);
							var spawnedUnit = unitProvider.SpawnLocalEntityWithArguments(new UnitProvider.Create
							{
								Direction       = team.TeamIndex <= 1 ? UnitDirection.Right : UnitDirection.Left,
								MovableCollider = capsuleColl,
								Mass            = PhysicsMass.CreateKinematic(capsuleColl.Value.MassProperties),
								Settings        = entityMgr.GetComponentData<UnitStatistics>(units[unt].Value)
							});

							entityMgr.AddComponent(spawnedUnit, typeof(GhostEntity));
							if (entityMgr.HasComponent<Relative<PlayerDescription>>(units[unt].Value))
							{
								entityMgr.ReplaceOwnerData(spawnedUnit, entityMgr.GetComponentData<Relative<PlayerDescription>>(units[unt].Value).Target);
							}
							else
							{
								// todo: entityMgr.AddComponent(spawnedUnit, typeof(BotControlledUnit));
							}

							var stat = entityMgr.GetComponentData<UnitStatistics>(units[unt].Value);
							var healthEntity = worldOrigin.GetExistingSystem<DefaultHealthData.InstanceProvider>().SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance
							{
								max   = stat.Health,
								value = stat.Health,
								owner = spawnedUnit
							});
							entityMgr.AddComponent(healthEntity, typeof(GhostEntity));
							MasterServerAbilities.Convert(dummySystem, spawnedUnit, entityMgr.GetBuffer<UnitDefinedAbilities>(units[unt].Value));

							onEntityCreated(entities[form], form, armies[arm].Value, arm, spawnedUnit, worldOrigin);
						}
					}
				}
			}

			if (wasFormationQueryNull)
				formationQuery.Dispose();
		}
	}
}