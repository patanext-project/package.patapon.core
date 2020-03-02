using System;
using Systems.GamePlay.CYari;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.Physics;
using Patapon.Mixed.RhythmEngine;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;

namespace Systems.GamePlay.CKibadda
{
	public struct KibaddaRushDefend : IComponentData
	{
		public float  AttackDelay;
		public Entity AttackHitBox;

		public class Provider : BaseRhythmAbilityProvider<KibaddaRushDefend>
		{
			public override    string MasterServerId   => nameof(P4OfficialAbilities.KibaRushDefend);
			public override    Type   ChainingCommand  => typeof(DefendCommand);
			protected override string file_path_prefix => "kiba";

			public override void SetEntityData(Entity entity, CreateAbility data)
			{
				base.SetEntityData(entity, data);

				var hitbox = World.GetExistingSystem<HitBox.Provider>().SpawnLocalEntityWithArguments(new HitBox.Create
				{
					Source = data.Owner,
					Collider = BoxCollider.Create(new BoxGeometry
					{
						Center      = new float3(0, 0.5f, 0),
						Orientation = quaternion.identity,
						Size        = new float3(6, 1, 1)
					})
				});

				var hitboxStats = StatisticModifier.Default;
				hitboxStats.Attack *= 0.5f;

				EntityManager.AddComponentData(hitbox, new HitBoxAgainstEnemies {AllyBufferSource = data.Owner});
				EntityManager.AddComponentData(hitbox, new DamageFromStatisticFrame {UseValueFrom = data.Owner, Modifier = hitboxStats});
				EntityManager.SetComponentData(entity, new KibaddaRushDefend {AttackHitBox      = hitbox});
				EntityManager.SetComponentData(entity, new AbilityControlVelocity {TargetFromCursor = true});
			}
		}

		public class System : BaseAbilitySystem
		{
			protected override void OnUpdate()
			{
				var ecb  = new EntityCommandBuffer(Allocator.TempJob);
				var tick = ServerTick;

				var impl                    = new BasicUnitAbilityImplementation(this);
				var teamRelativeFromEntity  = GetComponentDataFromEntity<Relative<TeamDescription>>();
				var chargeCommandFromEntity = GetComponentDataFromEntity<ChargeCommand>();

				Entities.ForEach((ref KibaddaRushDefend ability, ref AbilityControlVelocity control, in AbilityState state, in AbilityEngineSet engineSet, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					var direction         = impl.UnitDirection[owner.Target];
					var controllerUpdater = impl.Controller.GetUpdater(owner.Target).Out(out var controller);
					var velocityUpdater   = impl.Velocity.GetUpdater(owner.Target).Out(out var velocity);

					ability.AttackDelay -= tick.Delta;

					var rushToTarget   = state.IsActive;
					var stayAtPosition = state.IsChaining && chargeCommandFromEntity.Exists(engineSet.PreviousCommand);

					// rush like your life depend on it
					if (state.IsActive && ability.AttackDelay <= 0)
					{
						ecb.Chain(ability.AttackHitBox)
						   .RemoveComponent<Disabled>()
						   .SetComponent(new HitBox
						   {
							   Source = owner.Target, DisableAt = UTick.AddMsNextFrame(tick, 100)
						   })
						   .SetComponent(new HitBoxAgainstEnemies
						   {
							   AllyBufferSource = teamRelativeFromEntity[owner.Target].Target
						   })
						   .SetBuffer<HitBoxHistory>();

						ability.AttackDelay = impl.UnitPlayState[owner.Target].AttackSpeed;
					}

					if (rushToTarget)
					{
						Debug.Log("defend");
						
						control.IsActive       = true;
						control.Acceleration   = 15f;
						control.TargetPosition = direction.Value * 8;
					}
					else if (stayAtPosition)
					{
						controller.ControlOverVelocity.x = true;
						velocity.Value.x                 = math.lerp(velocity.Value.x, 0, impl.UnitPlayState[owner.Target].GetAcceleration() * 30 * tick.Delta);
					}

					ecb.SetComponent(ability.AttackHitBox, new Translation {Value = impl.Translation[owner.Target].Value});

					controllerUpdater.CompareAndUpdate(controller);
					velocityUpdater.CompareAndUpdate(velocity);
				}).Run();

				ecb.Playback(EntityManager);
				ecb.Dispose();
			}
		}
	}
}