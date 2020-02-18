using System;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using Patapon.Mixed.Units.Statistics;
using Patapon4TLB.Core;
using Patapon4TLB.Default;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;

namespace Patapon.Server.GameModes.VSHeadOn
{
	public static class Utility
	{
		public static void CreateUnitsBase(ComponentSystemBase       dummySystem,
		                                   World                     worldOrigin,      EntityQuery        formationQuery,
		                                   Func<Entity, World, bool> isFormationValid, Func<Entity, bool> isArmyValid, Action<Entity, int, Entity, int, Entity, World> onEntityCreated)
		{
			var unitProvider = worldOrigin.GetExistingSystem<UnitProvider>();

			var entityMgr                             = worldOrigin.EntityManager;
			var wasFormationQueryNull                 = formationQuery == null;
			if (wasFormationQueryNull) formationQuery = entityMgr.CreateEntityQuery(typeof(GameFormationTag), typeof(FormationRoot));

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
							if (!entityMgr.HasComponent<Relative<PlayerDescription>>(units[unt].Value))
								continue;

							var capsuleColl = CapsuleCollider.Create(new CapsuleGeometry
							{
								Radius  = 0.5f,
								Vertex0 = 0,
								Vertex1 = math.up() * 1.6f
							});
							var spawnedUnit = unitProvider.SpawnLocalEntityWithArguments(new UnitProvider.Create
							{
								Direction       = team.TeamIndex <= 1 ? UnitDirection.Right : UnitDirection.Left,
								MovableCollider = capsuleColl,
								Mass            = PhysicsMass.CreateKinematic(capsuleColl.Value.MassProperties),
								Settings        = entityMgr.GetComponentData<UnitStatistics>(units[unt].Value)
							});

							entityMgr.AddComponent(spawnedUnit, typeof(GhostEntity));
							entityMgr.SetOrAddComponentData(spawnedUnit, entityMgr.GetComponentData<UnitCurrentKit>(units[unt].Value));
							entityMgr.SetOrAddComponentData(spawnedUnit, entityMgr.GetComponentData<UnitDisplayedEquipment>(units[unt].Value));

							if (entityMgr.TryGetComponentData(units[unt].Value, out Relative<PlayerDescription> relativePlayer))
							{
								entityMgr.ReplaceOwnerData(spawnedUnit, relativePlayer.Target);

								var childrenBuffer = entityMgr.GetBuffer<OwnerChild>(relativePlayer.Target).ToNativeArray(Allocator.Temp);
								for (var i = 0; i != childrenBuffer.Length; i++)
								{
									if (entityMgr.HasComponent(childrenBuffer[i].Child, typeof(RhythmEngineDescription)))
										entityMgr.SetOrAddComponentData(spawnedUnit, new Relative<RhythmEngineDescription>(childrenBuffer[i].Child));
									if (entityMgr.HasComponent(childrenBuffer[i].Child, typeof(UnitTargetDescription)))
										entityMgr.SetOrAddComponentData(spawnedUnit, new Relative<UnitTargetDescription>(childrenBuffer[i].Child));
								}

								childrenBuffer.Dispose();
							}

							entityMgr.AddComponent(spawnedUnit, typeof(UnitTargetControlTag));

							var stat = entityMgr.GetComponentData<UnitStatistics>(units[unt].Value);
							var healthEntity = worldOrigin.GetExistingSystem<DefaultHealthData.InstanceProvider>().SpawnLocalEntityWithArguments(new DefaultHealthData.CreateInstance
							{
								max   = stat.Health,
								value = stat.Health,
								owner = spawnedUnit
							});
							entityMgr.AddComponent(healthEntity, typeof(GhostEntity));
							//if (entityMgr.HasComponent<Relative<RhythmEngineDescription>>(spawnedUnit))
							MasterServerAbilities.Convert(dummySystem, spawnedUnit, entityMgr.GetBuffer<UnitDefinedAbilities>(units[unt].Value));

							worldOrigin.GetExistingSystem<DefaultRebornAbility.Provider>()
							           .SpawnLocalEntityWithArguments(new CreateAbility
							           {
								           Owner = spawnedUnit,
							           });

							onEntityCreated(entities[form], form, armies[arm].Value, arm, spawnedUnit, worldOrigin);
						}
					}
				}
			}

			if (wasFormationQueryNull)
				formationQuery.Dispose();
		}

		public static Entity FindCommand(ComponentSystemBase system, Type type)
		{
			using (var query = system.EntityManager.CreateEntityQuery(type))
			{
				if (query.CalculateEntityCount() == 0)
					return Entity.Null;

				using (var entities = query.ToEntityArray(Allocator.TempJob))
				{
					return entities[0];
				}
			}
		}

		public static void RespawnUnit(EntityManager entityMgr, Entity unit, float3 spawnPointPos, bool firstSpawn = false)
		{
			var direction = entityMgr.GetComponentData<UnitDirection>(unit);
			var offset = 0f;
			if (firstSpawn)
			{
				offset += direction.Value * 10;
				if (entityMgr.GetComponentData<UnitCurrentKit>(unit).Value.Equals(UnitKnownTypes.Yarida))
					offset -= direction.Value * 1.5f;
				if (entityMgr.GetComponentData<UnitCurrentKit>(unit).Value.Equals(UnitKnownTypes.Yumiyacha))
					offset -= direction.Value * 3f;
			}
			
			entityMgr.SetComponentData(unit, new Translation
			{
				Value = new float3(spawnPointPos.x + offset, 0, 0)
			});

			var unitTargetRelative = entityMgr.GetComponentData<Relative<UnitTargetDescription>>(unit).Target;
			entityMgr.SetComponentData(unitTargetRelative, new Translation {Value = spawnPointPos.x + offset});
			entityMgr.SetOrAddComponentData(unitTargetRelative, direction);
			entityMgr.SetOrAddComponentData(unitTargetRelative, entityMgr.GetComponentData<Relative<TeamDescription>>(unit));
		}
	}
}