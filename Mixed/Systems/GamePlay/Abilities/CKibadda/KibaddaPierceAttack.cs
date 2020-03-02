using System;
using Systems.GamePlay.CYari;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.Physics;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
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
	public struct KibaddaPierceAttack : IComponentData
	{
		public float AttackDelay;
		public UTick AttackTick;

		public Entity AttackHitBox;

		public class Provider : BaseRhythmAbilityProvider<KibaddaPierceAttack>
		{
			public override    string MasterServerId   => nameof(P4OfficialAbilities.KibaPierceAttack);
			public override    Type   ChainingCommand  => typeof(AttackCommand);
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

				EntityManager.AddComponentData(hitbox, new HitBoxAgainstEnemies {AllyBufferSource = data.Owner});
				EntityManager.AddComponentData(hitbox, new DamageFromStatisticFrame {UseValueFrom = data.Owner, Modifier = hitboxStats});
				EntityManager.SetComponentData(entity, new KibaddaPierceAttack {AttackHitBox      = hitbox});
				EntityManager.SetComponentData(entity, new AbilityControlVelocity {TargetFromCursor = true});
			}
		}

		public class System : BaseAbilitySystem
		{
			protected override void OnUpdate()
			{
				var ecb  = new EntityCommandBuffer(Allocator.TempJob);
				var tick = ServerTick;

				var impl        = new BasicUnitAbilityImplementation(this);
				var seekEnemies = new SeekEnemies(this);

				var teamRelativeFromEntity = GetComponentDataFromEntity<Relative<TeamDescription>>();

				Entities.ForEach((ref KibaddaPierceAttack ability, ref AbilityControlVelocity control, in AbilityState state, in AbilityEngineSet engineSet, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;

					var seekState = seekEnemies.SeekingState[owner.Target];

					var direction         = impl.UnitDirection[owner.Target];
					var controllerUpdater = impl.Controller.GetUpdater(owner.Target).Out(out var controller);
					var velocityUpdater   = impl.Velocity.GetUpdater(owner.Target).Out(out var velocity);

					var rushToTarget = ability.AttackTick > 0 || seekState.Enemy != default && state.IsActive;
					if (rushToTarget && seekState.Enemy != default)
					{
						control.IsActive       = true;
						control.Acceleration   = 17.5f;
						control.TargetPosition = direction.Value * 14;
					}
					else if ((seekState.Enemy == default || state.IsChaining) && state.IsActiveOrChaining)
					{
						controller.ControlOverVelocity.x = true;
						velocity.Value.x                 = math.lerp(velocity.Value.x, 0, impl.UnitPlayState[owner.Target].GetAcceleration() * 30 * tick.Delta);
					}

					ability.AttackDelay -= tick.Delta;

					// rush like your life depend on it
					if (state.IsActive && ability.AttackDelay <= 0 && seekState.SelfEnemy != default && seekState.SelfDistance < 3)
					{
						ability.AttackTick  = tick;
						ability.AttackDelay = impl.UnitPlayState[owner.Target].AttackSpeed;
					}

					if (ability.AttackTick > 0 && UTick.AddMsNextFrame(ability.AttackTick, 200) > tick)
					{
						ecb.Chain(ability.AttackHitBox)
						   .RemoveComponent<Disabled>()
						   .SetComponent(new HitBox
						   {
							   Source = owner.Target, DisableAt = UTick.AddMsNextFrame(tick, 50)
						   })
						   .SetComponent(new HitBoxAgainstEnemies
						   {
							   AllyBufferSource = teamRelativeFromEntity[owner.Target].Target
						   })
						   .SetBuffer<HitBoxHistory>();

						ability.AttackTick = default;
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