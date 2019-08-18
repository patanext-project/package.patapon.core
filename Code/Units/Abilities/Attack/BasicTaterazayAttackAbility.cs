using package.patapon.core;
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
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Patapon4TLB.Default.Attack
{ 
	public struct BasicTaterazayAttackAbility : IComponentData
	{
		public const int DelaySlashMs = 100;

		public bool HasSlashed;

		public uint  AttackStartTick;
		public float NextAttackDelay;

		[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
		public class Process : JobGameBaseSystem
		{
			private struct Job : IJobForEach<RhythmAbilityState, BasicTaterazayAttackAbility, Relative<TeamDescription>, Relative<UnitTargetDescription>, Owner>
			{
				public UTick Tick;

				public NativeList<TargetDamageEvent> DamageEventList;

				[ReadOnly] public ComponentDataFromEntity<LivableHealth> LivableHealthFromEntity;

				[ReadOnly] public ComponentDataFromEntity<UnitStatistics> UnitSettingsFromEntity;
				[ReadOnly] public ComponentDataFromEntity<UnitPlayState>  UnitPlayStateFromEntity;

				public ComponentDataFromEntity<Translation>         TranslationFromEntity;
				public ComponentDataFromEntity<UnitControllerState> ControllerFromEntity;
				public ComponentDataFromEntity<Velocity>            VelocityFromEntity;

				[ReadOnly] public ComponentDataFromEntity<PhysicsCollider> PhysicsColliderFromEntity;
				[ReadOnly] public BufferFromEntity<TeamEnemies>            EnemiesFromTeam;

				public JobPhysicsQuery HitQuery;
				public SeekEnemies     SeekEnemies;

				private unsafe void Slash(Entity origin, GameComboState comboState, DynamicBuffer<TeamEnemies> teamEnemies)
				{
					var sphereCollider = (SphereCollider*) HitQuery.Ptr;
					sphereCollider->Radius = 2.6f;

					var distanceInput = new ColliderDistanceInput
					{
						Collider    = (Collider*) sphereCollider,
						MaxDistance = 0f,
						// remove z depth
						Transform = new RigidTransform(quaternion.identity, TranslationFromEntity[origin].Value * new float3(1, 1, 0))
					};

					var damage = 0;
					if (UnitSettingsFromEntity.Exists(origin))
					{
						var unitStatistics = UnitSettingsFromEntity[origin];
						damage = unitStatistics.Attack;

						float dmgF = damage;
						if (comboState.IsFever)
						{
							dmgF *= 1.2f;
							if (comboState.Score >= 50)
								dmgF *= 1.2f;

							damage += (int) dmgF - damage;
						}
					}

					for (var team = 0; team != teamEnemies.Length; team++)
					{
						var entities = SeekEnemies.EntitiesFromTeam[teamEnemies[team].Target];
						for (var ent = 0; ent != entities.Length; ent++)
						{
							var entity = entities[ent].Value;
							if (LivableHealthFromEntity.Exists(entity) && LivableHealthFromEntity[entity].IsDead)
								continue;
							if (!SeekEnemies.HitShapeContainerFromEntity.Exists(entity))
								continue;

							var hitShapeBuffer = SeekEnemies.HitShapeContainerFromEntity[entity];
							for (int i = 0, length = hitShapeBuffer.Length; i != length; i++)
							{
								var hitShape  = hitShapeBuffer[i];
								var transform = SeekEnemies.LocalToWorldFromEntity[hitShape.Value];
								var collider  = PhysicsColliderFromEntity[hitShape.Value];

								var cc = new CustomCollide(collider, transform);
								if (hitShape.AttachedToParent) 
									cc.WorldFromMotion.pos   += SeekEnemies.LocalToWorldFromEntity[entity].Position;
								// remove z depth
								cc.WorldFromMotion.pos.z =  0;

								var collection = new CustomCollideCollection(cc);
								var collector  = new ClosestHitCollector<DistanceHit>(1.0f);

								if (!collection.CalculateDistance(distanceInput, ref collector))
									continue;

								DamageEventList.Add(new TargetDamageEvent
								{
									Position    = cc.WorldFromMotion.pos + collider.ColliderPtr->CalculateAabb().Center,
									Origin      = origin,
									Destination = entity,
									Damage      = -damage
								});
								break;
							}
						}
					}
				}

				public void Execute(ref            RhythmAbilityState              state, ref BasicTaterazayAttackAbility ability,
				                    [ReadOnly] ref Relative<TeamDescription>       teamRelative,
				                    [ReadOnly] ref Relative<UnitTargetDescription> targetRelative,
				                    [ReadOnly] ref Owner                           owner)
				{
					const float attackRange = 3f;

					var teamEnemies = EnemiesFromTeam[teamRelative.Target];
					var statistics  = UnitSettingsFromEntity[owner.Target];
					var playState   = UnitPlayStateFromEntity[owner.Target];

					SeekEnemies.Execute
					(
						TranslationFromEntity[targetRelative.Target].Value, statistics.AttackSeekRange, teamEnemies,
						out var nearestEnemy, out var targetPosition, out var enemyDistance
					);

					if (state.IsStillChaining && !state.IsActive && nearestEnemy != default)
					{
						var velocity     = VelocityFromEntity[owner.Target];
						var acceleration = math.clamp(math.rcp(playState.Weight), 0, 1) * 50;
						acceleration = math.min(acceleration * Tick.Delta, 1);

						velocity.Value.x = math.lerp(velocity.Value.x, 0, acceleration);

						VelocityFromEntity[owner.Target] = velocity;
					}

					if (state.IsStillChaining && nearestEnemy != default)
					{
						var controller = ControllerFromEntity[owner.Target];
						controller.ControlOverVelocity.x   = true;
						ControllerFromEntity[owner.Target] = controller;
					}

					var attackStartTick = UTick.CopyDelta(Tick, ability.AttackStartTick);

					ability.NextAttackDelay -= Tick.Delta;
					if (ability.AttackStartTick > 0)
					{
						if (Tick >= UTick.AddMs(attackStartTick, DelaySlashMs) && !ability.HasSlashed)
						{
							ability.HasSlashed = true;

							Slash(owner.Target, state.Combo, teamEnemies);
						}

						// stop moving
						if (ability.HasSlashed)
						{
							var velocity     = VelocityFromEntity[owner.Target];
							var acceleration = math.clamp(math.rcp(playState.Weight), 0, 1) * 150;
							acceleration = math.min(acceleration * Tick.Delta, 1);

							velocity.Value.x = math.lerp(velocity.Value.x, 0, acceleration);

							VelocityFromEntity[owner.Target] = velocity;
						}

						// stop attacking once the animation is done
						if (Tick >= UTick.AddMs(attackStartTick, 500))
							ability.AttackStartTick = 0;
					}

					// if inactive or no enemy are present, continue...
					if (!state.IsActive || nearestEnemy == default)
						return;

					if (state.Combo.IsFever)
					{
						playState.MovementAttackSpeed *= 1.8f;
						if (state.Combo.Score >= 50)
							playState.MovementAttackSpeed *= 1.2f;
					}

					// if all conditions are ok, start attacking.
					enemyDistance = math.distance(TranslationFromEntity[owner.Target].Value.x, targetPosition.x);
					if (enemyDistance <= attackRange && ability.NextAttackDelay <= 0.0f && ability.AttackStartTick <= 0)
					{
						var atkSpeed = playState.AttackSpeed;
						if (state.Combo.IsFever && state.Combo.Score >= 50)
						{
							atkSpeed *= 0.75f;
						}

						ability.NextAttackDelay = atkSpeed;
						ability.AttackStartTick = (uint) Tick.Value;
						ability.HasSlashed      = false;
					}
					else if (Tick >= UTick.AddMs(attackStartTick, DelaySlashMs))
					{
						var controller = ControllerFromEntity[owner.Target];
						controller.ControlOverVelocity.x = true;

						ControllerFromEntity[owner.Target] = controller;

						var velocity     = VelocityFromEntity[owner.Target];
						var acceleration = math.clamp(math.rcp(playState.Weight), 0, 1) * 50;
						acceleration = math.min(acceleration * Tick.Delta, 1);

						var direction = math.sign(targetPosition.x - TranslationFromEntity[owner.Target].Value.x);
						velocity.Value.x = math.lerp(velocity.Value.x, playState.MovementAttackSpeed * direction, acceleration);

						VelocityFromEntity[owner.Target] = velocity;
					}
				}
			}

			private TargetDamageEvent.Provider m_DamageEventProvider;
			private JobPhysicsQuery m_HitQuery;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_DamageEventProvider = World.GetOrCreateSystem<TargetDamageEvent.Provider>();
				m_HitQuery            = new JobPhysicsQuery(() => SphereCollider.Create(0, 3f));
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				inputDeps = new Job
				{
					Tick            = World.GetExistingSystem<ServerSimulationSystemGroup>().GetTick(),
					DamageEventList = m_DamageEventProvider.GetEntityDelayedList(),

					LivableHealthFromEntity = GetComponentDataFromEntity<LivableHealth>(true),

					EnemiesFromTeam           = GetBufferFromEntity<TeamEnemies>(true),
					PhysicsColliderFromEntity = GetComponentDataFromEntity<PhysicsCollider>(true),

					UnitSettingsFromEntity  = GetComponentDataFromEntity<UnitStatistics>(true),
					UnitPlayStateFromEntity = GetComponentDataFromEntity<UnitPlayState>(true),
					ControllerFromEntity    = GetComponentDataFromEntity<UnitControllerState>(),
					TranslationFromEntity   = GetComponentDataFromEntity<Translation>(),
					VelocityFromEntity      = GetComponentDataFromEntity<Velocity>(),

					HitQuery    = m_HitQuery,
					SeekEnemies = new SeekEnemies(this)
				}.ScheduleSingle(this, inputDeps);

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
					typeof(DestroyChainReaction),
					typeof(PlayEntityTag),
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