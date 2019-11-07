using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon.Mixed.GamePlay;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Patapon.Server.GameModes.VSHeadOn
{
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public unsafe class HeadOnStructureCaptureProcess : JobGameBaseSystem
	{
		[BurstCompile]
		private struct JobCapture : IJobForEachWithEntity<LocalToWorld, HeadOnStructure, CaptureAreaComponent, PhysicsCollider, Relative<TeamDescription>>
		{
			public UTick Tick;

			[ReadOnly] public NativeArray<Entity>                         TeamArray;
			[ReadOnly] public BufferFromEntity<TeamEntityContainer>       EntitiesFromTeam;
			[ReadOnly] public ComponentDataFromEntity<LocalToWorld>       LtwFromEntity;
			[ReadOnly] public ComponentDataFromEntity<MovableDescription> MovableDescFromEntity;
			[ReadOnly] public ComponentDataFromEntity<PhysicsCollider>    PhysicsColliderFromEntity;

			public NativeList<HeadOnStructureOnCapture> CreateEventList;

			public void Execute(Entity                                   entity, int index,
			                    [ReadOnly] ref LocalToWorld              localToWorld,
			                    ref            HeadOnStructure           structure, [ReadOnly] ref CaptureAreaComponent captureArea,
			                    [ReadOnly] ref PhysicsCollider           collider,
			                    ref            Relative<TeamDescription> relativeTeam)
			{
				if (relativeTeam.Target != default)
					return;

				var cc = new CustomCollide(collider, localToWorld);
				var collection = new CustomCollideCollection(ref cc);
				if (captureArea.CaptureType == CaptureAreaType.Instant || structure.TimeToCapture <= 1)
				{
					structure.CaptureProgress[0] = 0;
					structure.CaptureProgress[1] = 0;

					for (var t = 0; t != TeamArray.Length; t++)
					{
						var entities = EntitiesFromTeam[TeamArray[t]];
						for (int ent = 0, entCount = entities.Length; ent < entCount; ent++)
						{
							if (!MovableDescFromEntity.Exists(entities[ent].Value)
							    || !PhysicsColliderFromEntity.Exists(entities[ent].Value))
								continue;
							var otherTransform = LtwFromEntity[entities[ent].Value];
							var otherCollider  = PhysicsColliderFromEntity[entities[ent].Value];

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

					relativeTeam.Target = structure.CaptureProgress[0] > structure.CaptureProgress[1] ? TeamArray[0] : TeamArray[1];
					CreateEventList.Add(new HeadOnStructureOnCapture
					{
						Source = entity
					});
				}
				else
				{
					var playerOnPointCount = stackalloc int[2];
					for (var t = 0; t != TeamArray.Length; t++)
					{
						var entities = EntitiesFromTeam[TeamArray[t]];
						for (int ent = 0, entCount = entities.Length; ent < entCount; ent++)
						{
							if (!MovableDescFromEntity.Exists(entities[ent].Value)
							    || !PhysicsColliderFromEntity.Exists(entities[ent].Value))
								continue;
							var otherTransform = LtwFromEntity[entities[ent].Value];
							var otherCollider  = PhysicsColliderFromEntity[entities[ent].Value];

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
					for (var t = 0; t != TeamArray.Length; t++)
					{
						var speed = playerOnPointCount[t] - playerOnPointCount[1 - t];
						if (speed < 0)
							continue;
						structure.CaptureProgress[t] += (int) (Tick.Delta * 1000 * speed);
					}

					for (var t = 0; t != TeamArray.Length; t++)
					{
						if (structure.CaptureProgress[t] >= structure.TimeToCapture
						    && playerOnPointCount[t] > playerOnPointCount[1 - t])
						{
							relativeTeam.Target = TeamArray[t];
							CreateEventList.Add(new HeadOnStructureOnCapture
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
			}
		}

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
			m_StructureQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new[]
				{
					ComponentType.ReadWrite<HeadOnStructure>(),
					ComponentType.ReadWrite<CaptureAreaComponent>(),
					ComponentType.ReadWrite<PhysicsCollider>(),
					ComponentType.ReadWrite<Relative<TeamDescription>>(),
					ComponentType.ReadWrite<LocalToWorld>()
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

			inputDeps = new JobCapture
			{
				Tick = GetTick(true),

				TeamArray                 = m_TempTeamArray,
				EntitiesFromTeam          = GetBufferFromEntity<TeamEntityContainer>(true),
				LtwFromEntity             = GetComponentDataFromEntity<LocalToWorld>(true),
				MovableDescFromEntity     = GetComponentDataFromEntity<MovableDescription>(true),
				PhysicsColliderFromEntity = GetComponentDataFromEntity<PhysicsCollider>(true),
				CreateEventList           = World.GetExistingSystem<MpVersusHeadOnGameMode>().CaptureEvents
			}.ScheduleSingle(m_StructureQuery, inputDeps);

			return inputDeps;
		}
	}
}