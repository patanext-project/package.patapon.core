using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.GamePlay;
using Unity.NetCode;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
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
			var ltwFromEntity             = GetComponentDataFromEntity<LocalToWorld>(true);
			var movableDescFromEntity     = GetComponentDataFromEntity<MovableDescription>(true);
			var physicsColliderFromEntity = GetComponentDataFromEntity<PhysicsCollider>(true);
			var createEventsList          = World.GetExistingSystem<MpVersusHeadOnGameMode>().CaptureEvents;
			inputDeps = Entities.ForEach((Entity entity, ref HeadOnStructure structure, ref Relative<TeamDescription> relativeTeam, in CaptureAreaComponent captureArea, in LocalToWorld ltw, in PhysicsCollider collider) =>
			{
				if (relativeTeam.Target != default)
					return;

				var cc         = new CustomCollide(collider, ltw);
				var collection = new CustomCollideCollection(ref cc);
				if (captureArea.CaptureType == CaptureAreaType.Instant || structure.TimeToCapture <= 1)
				{
					structure.CaptureProgress[0] = 0;
					structure.CaptureProgress[1] = 0;

					for (var t = 0; t != teamArray.Length; t++)
					{
						var entities = entitiesFromTeam[teamArray[t]];
						for (int ent = 0, entCount = entities.Length; ent < entCount; ent++)
						{
							if (!movableDescFromEntity.Exists(entities[ent].Value)
							    || !physicsColliderFromEntity.Exists(entities[ent].Value))
								continue;
							var otherTransform = ltwFromEntity[entities[ent].Value];
							var otherCollider  = physicsColliderFromEntity[entities[ent].Value];

							var input = new ColliderDistanceInput
							{
								Collider    = otherCollider.ColliderPtr,
								MaxDistance = 0f,
								Transform   = new RigidTransform(otherTransform.Value)
							};

							var collector = new ClosestHitCollector<DistanceHit>(0);
							if (!collection.CalculateDistance(input, ref collector))
								continue;

							structure.CaptureProgress[t] = 1;
						}
					}

					// equal...
					if (structure.CaptureProgress[0] == structure.CaptureProgress[1])
						return;

					relativeTeam.Target = structure.CaptureProgress[0] > structure.CaptureProgress[1] ? teamArray[0] : teamArray[1];
					createEventsList.Add(new HeadOnStructureOnCapture
					{
						Source = entity
					});
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
							    || !physicsColliderFromEntity.Exists(entities[ent].Value))
								continue;
							var otherTransform = ltwFromEntity[entities[ent].Value];
							var otherCollider  = physicsColliderFromEntity[entities[ent].Value];

							var input = new ColliderDistanceInput
							{
								Collider    = otherCollider.ColliderPtr,
								MaxDistance = 0f,
								Transform   = new RigidTransform(otherTransform.Value)
							};

							var collector = new ClosestHitCollector<DistanceHit>(0);
							if (!collection.CalculateDistance(input, ref collector))
								continue;

							playerOnPointCount[t]++;
						}
					}

					// Apply capture progression...
					for (var t = 0; t != teamArray.Length; t++)
					{
						var speed = playerOnPointCount[t] - playerOnPointCount[1 - t];
						if (speed < 0)
							continue;
						structure.CaptureProgress[t] += (int) (tick.Delta * 1000 * speed);
					}

					for (var t = 0; t != teamArray.Length; t++)
					{
						if (structure.CaptureProgress[t] >= structure.TimeToCapture
						    && playerOnPointCount[t] > playerOnPointCount[1 - t])
						{
							relativeTeam.Target = teamArray[t];
							createEventsList.Add(new HeadOnStructureOnCapture
							{
								Source = entity
							});

							return;
						}

						// Limit other team capture progress
						if (playerOnPointCount[t] > playerOnPointCount[1 - t])
							structure.CaptureProgress[1 - t] = math.min(structure.CaptureProgress[1 - t], structure.TimeToCapture - structure.CaptureProgress[t]);
					}
				}
			}).Schedule(inputDeps);

			return inputDeps;
		}
	}
}