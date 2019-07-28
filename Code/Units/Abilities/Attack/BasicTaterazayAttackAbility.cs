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

namespace Patapon4TLB.Default.Attack
{
	public struct BasicTaterazayAttackAbility : IComponentData
	{
		public const int DelayBeforeSlash = 250;
		
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

				[ReadOnly]
				public ComponentDataFromEntity<UnitBaseSettings> UnitSettingsFromEntity;

				[NativeDisableParallelForRestriction]
				public ComponentDataFromEntity<Translation> TranslationFromEntity;

				[DeallocateOnJobCompletion]
				public NativeArray<ArchetypeChunk> LivableChunks;

				public ArchetypeChunkEntityType                     EntityType;
				public ArchetypeChunkComponentType<LocalToWorld>    LtwType;
				public ArchetypeChunkComponentType<PhysicsCollider> ColliderType;

				public JobPhysicsQuery HitQuery;

				private unsafe void Slash(Entity origin)
				{
					var boxCollider = (BoxCollider*) HitQuery.Ptr;
					boxCollider->Size = new float3(3, 2, 1);

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
							if (collection.CalculateDistance(distanceInput, ref collector))
							{
								Debug.Log("yay");
								DamageEventList.Add(new TargetDamageEvent
								{
									Position    = collector.ClosestHit.Position,
									Origin      = origin,
									Destination = entity,
									Damage      = 42
								});
							}
							else
							{
								Debug.Log("nay");
							}
						}
					}
				}

				public void Execute(ref RhythmAbilityState state, ref BasicTaterazayAttackAbility ability, [ReadOnly] ref Owner owner)
				{
					ability.NextAttackDelay -= DeltaTime;
					if (ability.AttackStartTime >= 0)
					{
						if (ability.AttackStartTime + DelayBeforeSlash < Tick && !ability.HasSlashed)
						{
							ability.HasSlashed = true;
							Debug.Log(Tick + " >  slash!");

							Slash(owner.Target);
						}

						// stop attacking once the animation is done
						if (ability.AttackStartTime + 500 < Tick)
							ability.AttackStartTime = -1;
					}

					if (!state.IsActive)
						return;

					var settings = UnitSettingsFromEntity[owner.Target];
					if (ability.NextAttackDelay <= 0.0f && ability.AttackStartTime < 0)
					{
						ability.NextAttackDelay = settings.AttackSpeed;
						ability.AttackStartTime = Tick;
						ability.HasSlashed      = false;

						Debug.Log(Tick + " >  start attack!");
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
				m_HitQuery            = new JobPhysicsQuery(() => BoxCollider.Create(0, quaternion.identity, 1, 0.1f));
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				inputDeps = new Job
				{
					Tick      = GetSingleton<GameTimeComponent>().Tick,
					DeltaTime = GetSingleton<GameTimeComponent>().DeltaTime,

					DamageEventList = m_DamageEventProvider.GetEntityDelayedList(),

					UnitSettingsFromEntity = GetComponentDataFromEntity<UnitBaseSettings>(true),
					TranslationFromEntity  = GetComponentDataFromEntity<Translation>(),

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