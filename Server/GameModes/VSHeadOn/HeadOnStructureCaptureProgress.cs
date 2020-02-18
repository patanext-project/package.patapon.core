using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.GamePlay;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[AlwaysSynchronizeSystem]
	public unsafe class HeadOnStructureCaptureProcess : JobGameBaseSystem
	{
		private EntityQuery m_GameModeQuery;
		private EntityQuery m_StructureQuery;

		private NativeArray<Entity> m_TempTeamArray;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_GameModeQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new[]
				{
					ComponentType.ReadWrite<ExecutingGameMode>(),
					ComponentType.ReadWrite<MpVersusHeadOn>()
				}
			});
			m_TempTeamArray = new NativeArray<Entity>(2, Allocator.Persistent);
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (m_GameModeQuery.CalculateEntityCount() == 0)
				return inputDeps;
			var gameModeData = EntityManager.GetComponentData<MpVersusHeadOn>(m_GameModeQuery.GetSingletonEntity());
			if (gameModeData.PlayState != MpVersusHeadOn.State.Playing)
				return inputDeps;

			m_TempTeamArray[0] = gameModeData.Team0;
			m_TempTeamArray[1] = gameModeData.Team1;

			var tick                      = GetTick(true);
			var teamArray                 = m_TempTeamArray;
			var entitiesFromTeam          = GetBufferFromEntity<TeamEntityContainer>(true);
			var healthFromEntity = GetComponentDataFromEntity<LivableHealth>(true);
			var ltwFromEntity             = GetComponentDataFromEntity<LocalToWorld>(true);
			var movableDescFromEntity     = GetComponentDataFromEntity<MovableDescription>(true);
			var physicsColliderFromEntity = GetComponentDataFromEntity<PhysicsCollider>(true);
			var createEventsList          = World.GetExistingSystem<MpVersusHeadOnGameMode>().CaptureEvents;
			var relativeTeamFromEntity    = GetComponentDataFromEntity<Relative<TeamDescription>>();

			Entities
				.WithReadOnly(entitiesFromTeam)
				.WithReadOnly(ltwFromEntity)
				.WithReadOnly(healthFromEntity)
				.WithReadOnly(movableDescFromEntity)
				.WithReadOnly(physicsColliderFromEntity)
				.ForEach((Entity entity, ref HeadOnStructure structure, ref CaptureAreaComponent captureArea, in LocalToWorld ltw, in PhysicsCollider collider) =>
				{
					var relativeTeamUpdater = relativeTeamFromEntity.GetUpdater(entity)
					                                                .Out(out var relativeTeam);
					if (!relativeTeamUpdater.possess || relativeTeam.Target != default)
						return;

					var cc         = new CustomCollide(collider, ltw);
					var aabb = cc.Collider->CalculateAabb(cc.WorldFromMotion);
					aabb.Expand(0.1f);

					captureArea.Aabb = aabb;
					
					if (captureArea.CaptureType == CaptureAreaType.Instant || structure.TimeToCapture <= 1)
					{
						structure.CaptureProgress[0] = 0;
						structure.CaptureProgress[1] = 0;
						
						//structure.CaptureProgress[1] = 1;

						for (var t = 0; t != teamArray.Length; t++)
						{
							var entities = entitiesFromTeam[teamArray[t]];
							for (int ent = 0, entCount = entities.Length; ent < entCount; ent++)
							{
								if (!movableDescFromEntity.Exists(entities[ent].Value)
								    || !physicsColliderFromEntity.Exists(entities[ent].Value)
								    || healthFromEntity.TryGet(entities[ent].Value, out var health) && health.IsDead)
									continue;

								if (!aabb.Contains(ltwFromEntity[entities[ent].Value].Position))
									continue;
								
								structure.CaptureProgress[t] = 1;
							}
						}

						// equal...
						if (structure.CaptureProgress[0] == structure.CaptureProgress[1])
							return;

						relativeTeam.Target = structure.CaptureProgress[0] > structure.CaptureProgress[1] ? teamArray[0] : teamArray[1];
						relativeTeamUpdater.Update(relativeTeam);

						createEventsList.Add(new HeadOnStructureOnCapture
						{
							Source = entity
						});
						
						structure.CaptureProgress[0] = 0;
						structure.CaptureProgress[1] = 0;
					}
					else
					{
						var playerOnPointCount = stackalloc int[2];
						for (var t = 0; t != teamArray.Length; t++)
						{
							var entities = entitiesFromTeam[teamArray[t]];
							for (int ent = 0, entCount = entities.Length; ent < entCount; ent++)
							{
								if (!movableDescFromEntity.Exists(entities[ent].Value)
								    || !physicsColliderFromEntity.Exists(entities[ent].Value)
								    || healthFromEntity.TryGet(entities[ent].Value, out var health) && health.IsDead)
									continue;

								if (!aabb.Contains(ltwFromEntity[entities[ent].Value].Position))
									continue;

								playerOnPointCount[t]++;
							}
						}

						//playerOnPointCount[1] = 8;

						var initialProgress = stackalloc[] {structure.CaptureProgress[0], structure.CaptureProgress[1]};

						// Apply capture progression...
						for (var t = 0; t != teamArray.Length; t++)
						{
							var speed = playerOnPointCount[t];
							var otherCount = playerOnPointCount[1 - t];
							// block the early progression if it shouldn't go further against the other team progression...
							if (initialProgress[t] + speed >= structure.TimeToCapture - structure.CaptureProgress[1 - t]
							    && otherCount >= speed)
								speed = 0;

							if (speed <= 0)
								continue;
							structure.CaptureProgress[t] += (int) (tick.DeltaMs * speed);
						}

						for (var t = 0; t != teamArray.Length; t++)
						{
							if (structure.CaptureProgress[t] >= structure.TimeToCapture
							    && playerOnPointCount[t] > playerOnPointCount[1 - t])
							{
								relativeTeam.Target = teamArray[t];
								relativeTeamUpdater.Update(relativeTeam);
								createEventsList.Add(new HeadOnStructureOnCapture
								{
									Source = entity
								});

								structure.CaptureProgress[0] = 0;
								structure.CaptureProgress[1] = 0;

								return;
							}

							// Limit other team capture progress
							if (playerOnPointCount[t] > playerOnPointCount[1 - t])
								structure.CaptureProgress[1 - t] = math.min(structure.CaptureProgress[1 - t], structure.TimeToCapture - structure.CaptureProgress[t]);
						}
					}
				}).Run();

			return default;
		}
	}
}