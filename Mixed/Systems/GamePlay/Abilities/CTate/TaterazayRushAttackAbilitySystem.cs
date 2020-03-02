using Systems.GamePlay.CYari;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.Abilities.CTate;
using Patapon.Mixed.Units;
using Patapon.Mixed.Utilities;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Systems.GamePlay.CTate
{
	public unsafe class TaterazayRushAttackAbilitySystem : BaseAbilitySystem
	{
		private TargetDamageEvent.Provider m_DamageEventProvider;
		private JobPhysicsQuery            m_HitQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_HitQuery = new JobPhysicsQuery(() => CapsuleCollider.Create(new CapsuleGeometry
			{
				Vertex0 = new float3(0, 0, 0),
				Vertex1 = new float3(0, 4, 0),
				Radius  = 2f
			}));
			m_DamageEventProvider = World.GetOrCreateSystem<TargetDamageEvent.Provider>();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			m_HitQuery.Dispose();
		}

		protected override void OnUpdate()
		{
			var tick                      = ServerTick;
			var enemiesFromTeam           = GetBufferFromEntity<TeamEnemies>(true);
			var physicsColliderFromEntity = GetComponentDataFromEntity<PhysicsCollider>(true);
			var impl                      = new BasicUnitAbilityImplementation(this);
			var seekEnemies               = new SeekEnemies(this);
			var seekingStateFromEntity    = GetComponentDataFromEntity<UnitEnemySeekingState>(true);

			var relativeTargetFromEntity = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);

			var colliderQuery          = m_HitQuery;
			var relativeTeamFromEntity = GetComponentDataFromEntity<Relative<TeamDescription>>(true);

			var damageEvArchetype = m_DamageEventProvider.EntityArchetype;
			var ecb               = m_DamageEventProvider.CreateEntityCommandBuffer();

			Entities
				.ForEach((Entity entity, int nativeThreadIndex, ref TaterazayRushAttackAbility ability, in AbilityState state, in AbilityEngineSet engineSet, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					var seekingState   = seekingStateFromEntity[owner.Target];
					var playState      = impl.UnitPlayState[owner.Target];
					var unitPosition   = impl.Translation[owner.Target].Value;
					var unitDirection  = impl.UnitDirection[owner.Target].Value;
					var relativeTarget = relativeTargetFromEntity[owner.Target].Target;

					var velocityUpdater   = impl.Velocity.GetUpdater(owner.Target).Out(out var velocity);
					var controllerUpdater = impl.Controller.GetUpdater(owner.Target).Out(out var controller);

					var attackStartTick = UTick.CopyDelta(tick, ability.AttackStartTick);

					if ((state.Phase & EAbilityPhase.Chaining) != 0)
					{
						controller.ControlOverVelocity.x = true;
						velocity.Value.x                 = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 50 * tick.Delta);

						ability.AttackStartTick = 0;
						ability.ForceAttackTick = 0;
						if (ability.Phase == TaterazayRushAttackAbility.EPhase.Run)
							ability.Phase = TaterazayRushAttackAbility.EPhase.Wait;
					}

					ability.NextAttackDelay -= tick.Delta;
					if (ability.Phase == TaterazayRushAttackAbility.EPhase.AttackRequested)
					{
						if (UTick.AddMsNextFrame(attackStartTick, TaterazayRushAttackAbility.AttackDelayMs) > tick)
						{
							ability.Phase = TaterazayRushAttackAbility.EPhase.Attacking;

							// todo: test to see if it work...
							controller.ControlOverVelocity.x = true;
							velocity.Value.y                 = 12.5f; // small jump xd
							velocity.Value.x                 = (-unitDirection) * 1;
						}
					}

					if (ability.Phase == TaterazayRushAttackAbility.EPhase.Attacking)
					{
						var distanceInput = CreateDistanceFlatInput.ColliderWithOffset(colliderQuery.Ptr, unitPosition.xy, new float2(unitDirection, 1));
						var allEnemies    = new NativeList<Entity>(Allocator.Temp);

						var tryGetChain = stackalloc[] {entity, owner.Target};
						if (!relativeTeamFromEntity.TryGetChain(tryGetChain, 2, out var relativeTeam))
							return;

						var teamEnemies = enemiesFromTeam[relativeTeam.Target];

						seekEnemies.GetAllEnemies(ref allEnemies, teamEnemies);

						var rigidBodies = new NativeList<RigidBody>(allEnemies.Length, Allocator.Temp);
						for (var ent = 0; ent != allEnemies.Length; ent++)
						{
							var enemy = allEnemies[ent];
							if (!seekEnemies.CanHitTarget(enemy))
								continue;

							rigidBodies.Clear();
							CreateRigidBody.Execute(ref rigidBodies, seekEnemies.HitShapeContainer[enemy].AsNativeArray(),
								enemy,
								impl.LocalToWorld, impl.Translation, physicsColliderFromEntity);
							for (var i = 0; i != rigidBodies.Length; i++)
							{
								var cc = new CustomCollide(rigidBodies[i]) {WorldFromMotion = {pos = {z = 0}}};
								if (!new CustomCollideCollection(ref cc).CalculateDistance(distanceInput, out var closestHit))
									continue;

								var evEnt = ecb.CreateEntity(damageEvArchetype);
								ecb.SetComponent(evEnt, new TargetDamageEvent
								{
									Origin = owner.Target, Destination = enemy, Damage = -playState.Attack
								});
								ecb.AddComponent(evEnt, new Translation {Value = closestHit.Position});
								break;
							}
						}

						// ofc, we shouldn't directly go waiting...
						ability.Phase           = TaterazayRushAttackAbility.EPhase.Wait;
						ability.NextAttackDelay = playState.AttackSpeed;
					}

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);

					// if inactive or no enemy are present, continue...
					if ((state.Phase & EAbilityPhase.Active) == 0)
						return;

					var statistics = impl.UnitSettings[owner.Target];

					controller.ControlOverVelocity.x = true;

					// Execute if:
					// - If we are near of an enemy or if the attack was forced.
					// - If we are in the RUN phase.
					if ((seekingState.Enemy != default && seekingState.SelfDistance <= 1f || tick >= ability.ForceAttackTick)
					    && ability.Phase == TaterazayRushAttackAbility.EPhase.Run)
					{
						ability.AttackStartTick = tick.AsUInt;
						ability.Phase           = TaterazayRushAttackAbility.EPhase.AttackRequested;
					}
					else if (tick >= UTick.AddMs(attackStartTick, TaterazayBasicAttackAbility.DelaySlashMs) && ability.NextAttackDelay <= 0)
					{
						var targetPosition = impl.Translation[relativeTarget].Value.x + 12.5f * unitDirection;
						velocity.Value.x = AbilityUtility.GetTargetVelocityX(new AbilityUtility.GetTargetVelocityParameters
							{
								TargetPosition   = targetPosition,
								PreviousPosition = unitPosition,
								PreviousVelocity = velocity.Value,
								PlayState        = playState,
								Acceleration     = 25,
								Tick             = tick
							},
							deaccel_distance: 0.0f, deaccel_distance_max: 0.5f);

						if (ability.Phase != TaterazayRushAttackAbility.EPhase.Run)
						{
							ability.ForceAttackTick = UTick.AddMsNextFrame(tick, 1000).AsUInt;
						}

						ability.Phase = TaterazayRushAttackAbility.EPhase.Run;
					}
					else
					{
						ability.Phase = TaterazayRushAttackAbility.EPhase.Wait;
					}

					velocityUpdater.CompareAndUpdate(velocity);
					controllerUpdater.CompareAndUpdate(controller);
				})
				.WithReadOnly(enemiesFromTeam)
				.WithReadOnly(physicsColliderFromEntity)
				.WithReadOnly(seekingStateFromEntity)
				.WithReadOnly(relativeTeamFromEntity)
				.Run();
		}
	}
}