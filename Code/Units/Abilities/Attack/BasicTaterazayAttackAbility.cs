using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Patapon4TLB.Default.Attack
{
	public struct BasicTaterazayAttackAbility : IComponentData
	{
		public const int DelayBeforeSlash = 100;
		
		public bool HasSlashed;

		public int AttackStartTime;

		public float NextAttackDelay;

		[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
		public class Process : JobGameBaseSystem
		{
			private struct Job : IJobForEach<RhythmAbilityState, BasicTaterazayAttackAbility, Owner>
			{
				public int   Tick;
				public float DeltaTime;

				public NativeList<TargetDamageEvent> DamageEventList;

				[ReadOnly] public ComponentDataFromEntity<UnitBaseSettings> UnitSettingsFromEntity;
				[ReadOnly] public ComponentDataFromEntity<UnitPlayState> UnitPlayStateFromEntity;
				
				public ComponentDataFromEntity<Translation> TranslationFromEntity;
				public ComponentDataFromEntity<UnitTargetPosition> TargetFromEntity;
				public ComponentDataFromEntity<UnitControllerState> ControllerFromEntity;
				public ComponentDataFromEntity<Velocity> VelocityFromEntity;

				[DeallocateOnJobCompletion]
				public NativeArray<ArchetypeChunk> LivableChunks;

				public ArchetypeChunkEntityType                     EntityType;
				public ArchetypeChunkComponentType<LocalToWorld>    LtwType;
				public ArchetypeChunkComponentType<PhysicsCollider> ColliderType;

				public JobPhysicsQuery HitQuery;

				private unsafe void Slash(Entity origin)
				{
					var boxCollider = (SphereCollider*) HitQuery.Ptr;
					boxCollider->Radius = 3;

					var distanceInput = new ColliderDistanceInput
					{
						Collider    = (Collider*) boxCollider,
						MaxDistance = 0.1f,
						Transform   = new RigidTransform(quaternion.identity, TranslationFromEntity[origin].Value)
					};

					for (var ch = 0; ch != LivableChunks.Length; ch++)
					{
						var chunk       = LivableChunks[ch];
						var entityArray = chunk.GetNativeArray(EntityType);
						var ltwArray    = chunk.GetNativeArray(LtwType);
						var collArray   = chunk.GetNativeArray(ColliderType);

						var count = chunk.Count;
						for (var ent = 0; ent != count; ent++)
						{
							var entity = entityArray[ent];
							if (origin == entity)
								continue; // lol no
							
							var transform = ltwArray[ent];
							var collider  = collArray[ent];

							var collection = new CustomCollideCollection(new CustomCollide(collider, transform));
							var collector  = new ClosestHitCollector<DistanceHit>(1.0f);
							if (!collection.CalculateDistance(distanceInput, ref collector)) 
								continue;
							
							DamageEventList.Add(new TargetDamageEvent
							{
								Position    = collector.ClosestHit.Position,
								Origin      = origin,
								Destination = entity,
								Damage      = 42
							});
						}
					}
				}

				public void SeekNearestEnemy(Entity origin, float seekRange, out Entity nearestEnemy, out float3 target, out float targetDistance)
				{
					nearestEnemy = Entity.Null;
					target = float3.zero;
					targetDistance = default;
					
					var shortestDistance = float.MaxValue;
					var originPos = TargetFromEntity[origin].Value;
					for (var ch = 0; ch != LivableChunks.Length; ch++)
					{
						var chunk       = LivableChunks[ch];
						var entityArray = chunk.GetNativeArray(EntityType);
						var ltwArray    = chunk.GetNativeArray(LtwType);

						var count = chunk.Count;
						for (var ent = 0; ent != count; ent++)
						{
							var entity = entityArray[ent];
							if (origin == entity)
								continue; // lol no

							var enemyTransform = ltwArray[ent];
							var distance       = math.distance(originPos, enemyTransform.Position);
							if (distance > seekRange)
								continue;

							if (distance > shortestDistance)
								continue;

							target           = enemyTransform.Position;
							targetDistance   = distance;
							shortestDistance = distance;
							nearestEnemy     = entity;
						}
					}
				}

				public void Execute(ref RhythmAbilityState state, ref BasicTaterazayAttackAbility ability, [ReadOnly] ref Owner owner)
				{
					const float seekRange = 20f;
					const float attackRange = 3.5f;
					
					SeekNearestEnemy(owner.Target, seekRange, out var nearestEnemy, out var targetPosition, out var enemyDistance);
					if (nearestEnemy != default && TranslationFromEntity[owner.Target].Value.x > targetPosition.x)
					{
						var tr = TranslationFromEntity[owner.Target];
						tr.Value.x = targetPosition.x;
						TranslationFromEntity[owner.Target] = tr;
					}

					var settings = UnitSettingsFromEntity[owner.Target];
					var playState = UnitPlayStateFromEntity[owner.Target];
					if (state.IsStillChaining && !state.IsActive && nearestEnemy != default)
					{
						var velocity     = VelocityFromEntity[owner.Target];
						var acceleration = math.clamp(math.rcp(playState.Weight), 0, 1) * 50;
						acceleration = math.min(acceleration * DeltaTime, 1);

						velocity.Value.x = math.lerp(velocity.Value.x, 0, acceleration);

						VelocityFromEntity[owner.Target] = velocity;
					}

					if (state.IsStillChaining && nearestEnemy != default)
					{
						var controller = ControllerFromEntity[owner.Target];
						controller.ControlOverVelocity.x   = true;
						ControllerFromEntity[owner.Target] = controller;						
					}

					ability.NextAttackDelay -= DeltaTime;
					if (ability.AttackStartTime >= 0)
					{
						if (ability.AttackStartTime + DelayBeforeSlash < Tick && !ability.HasSlashed)
						{
							ability.HasSlashed = true;
							Debug.Log(Tick + " >  slash!");

							Slash(owner.Target);
						}

						// stop moving
						if (ability.HasSlashed)
						{
							var velocity     = VelocityFromEntity[owner.Target];
							var acceleration = math.clamp(math.rcp(playState.Weight), 0, 1) * 150;
							acceleration = math.min(acceleration * DeltaTime, 1);

							velocity.Value.x = math.lerp(velocity.Value.x, 0, acceleration);

							VelocityFromEntity[owner.Target] = velocity;
						}

						// stop attacking once the animation is done
						if (ability.AttackStartTime + 500 < Tick)
							ability.AttackStartTime = -1;
					}

					if (!state.IsActive)
						return;

					// if no enemy are present, continue...
					if (nearestEnemy == default)
						return;
					
					// if all conditions are ok, start attacking.
					enemyDistance = math.distance(TranslationFromEntity[owner.Target].Value.x, targetPosition.x);
					if (enemyDistance <= attackRange && ability.NextAttackDelay <= 0.0f && ability.AttackStartTime < 0)
					{
						ability.NextAttackDelay = settings.AttackSpeed;
						ability.AttackStartTime = Tick;
						ability.HasSlashed      = false;

						Debug.Log(Tick + " >  start attack!");
					}
					else if (ability.AttackStartTime + DelayBeforeSlash < Tick)
					{
						var controller = ControllerFromEntity[owner.Target];
						controller.ControlOverVelocity.x = true;

						ControllerFromEntity[owner.Target] = controller;

						var velocity     = VelocityFromEntity[owner.Target];
						var acceleration = math.clamp(math.rcp(playState.Weight), 0, 1) * 50;
						acceleration = math.min(acceleration * DeltaTime, 1);

						velocity.Value.x = math.lerp(velocity.Value.x, playState.MovementAttackSpeed, acceleration);

						VelocityFromEntity[owner.Target] = velocity;
					}
				}
			}

			private TargetDamageEvent.Provider m_DamageEventProvider;
			private EntityQuery                m_LivableQuery;

			private JobPhysicsQuery m_HitQuery;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_DamageEventProvider = World.GetOrCreateSystem<TargetDamageEvent.Provider>();
				m_LivableQuery        = GetEntityQuery(typeof(LivableDescription), typeof(PhysicsCollider), typeof(LocalToWorld));
				m_HitQuery            = new JobPhysicsQuery(() => SphereCollider.Create(0, 3f));
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				inputDeps = new Job
				{
					Tick      = GetSingleton<GameTimeComponent>().Tick,
					DeltaTime = GetSingleton<GameTimeComponent>().DeltaTime,

					DamageEventList = m_DamageEventProvider.GetEntityDelayedList(),

					UnitSettingsFromEntity = GetComponentDataFromEntity<UnitBaseSettings>(true),
					UnitPlayStateFromEntity = GetComponentDataFromEntity<UnitPlayState>(true),
					ControllerFromEntity = GetComponentDataFromEntity<UnitControllerState>(),
					TargetFromEntity = GetComponentDataFromEntity<UnitTargetPosition>(),
					TranslationFromEntity  = GetComponentDataFromEntity<Translation>(),
					VelocityFromEntity = GetComponentDataFromEntity<Velocity>(),

					LivableChunks = m_LivableQuery.CreateArchetypeChunkArray(Allocator.TempJob, out var dep1),
					EntityType    = GetArchetypeChunkEntityType(),
					LtwType       = GetArchetypeChunkComponentType<LocalToWorld>(),
					ColliderType  = GetArchetypeChunkComponentType<PhysicsCollider>(),

					HitQuery = m_HitQuery
				}.ScheduleSingle(this, JobHandle.CombineDependencies(inputDeps, dep1));

				m_DamageEventProvider.AddJobHandleForProducer(inputDeps);

				return inputDeps;
			}
		}

		public struct Create
		{
			public Entity Owner;
			public Entity Command;
		}

		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(ActionDescription),
					typeof(RhythmAbilityState),
					typeof(BasicTaterazayAttackAbility),
					typeof(Owner),
					typeof(DestroyChainReaction)
				};
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.ReplaceOwnerData(entity, data.Owner);
				EntityManager.SetComponentData(entity, new RhythmAbilityState {Command = data.Command});
				EntityManager.SetComponentData(entity, new BasicTaterazayAttackAbility { });
				EntityManager.SetComponentData(entity, new Owner {Target = data.Owner});
				EntityManager.SetComponentData(entity, new DestroyChainReaction(data.Owner));
			}
		}
	}
}