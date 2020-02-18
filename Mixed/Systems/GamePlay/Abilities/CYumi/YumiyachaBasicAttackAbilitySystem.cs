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
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Systems.GamePlay.CYumi
{
	public struct YumiyachaBasicAttackAbility : IComponentData
	{
		public const uint ShootDelayMs  = 120;
		public const uint VolleyDelayMs = 150;

		public UTick NextAttack;
		public float NextAttackDelay;

		public int VolleyIndex;
		public int VolleyCount;

		public class Provider : BaseRhythmAbilityProvider<YumiyachaBasicAttackAbility>
		{
			public override string MasterServerId  => nameof(P4OfficialAbilities.YumiBasicAttack);
			public override Type   ChainingCommand => typeof(AttackCommand);

			public override void SetEntityData(Entity entity, CreateAbility data)
			{
				base.SetEntityData(entity, data);

				EntityManager.SetComponentData(entity, new YumiyachaBasicAttackAbility
				{
					VolleyCount = 3
				});
			}
		}
	}

	public class YumiyachaBasicAttackAbilitySystem : BaseAbilitySystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var impl = new BasicUnitAbilityImplementation(this);
			var seek = new SeekEnemies(this);

			var tick = ServerTick;

			var seekingStateFromEntity = GetComponentDataFromEntity<UnitEnemySeekingState>(true);

			Entities.ForEach((ref YumiyachaBasicAttackAbility ability, ref AbilityControlVelocity control, in AbilityState state, in Owner owner) =>
			{
				if ((state.Phase & EAbilityPhase.ActiveOrChaining) == 0)
					return;

				var seekingState = seekingStateFromEntity[owner.Target];
				var position     = impl.TranslationFromEntity[owner.Target].Value;
				var playState    = impl.UnitPlayStateFromEntity[owner.Target];
				var direction    = impl.UnitDirectionFromEntity[owner.Target].Value;

				var velocityUpdater   = impl.VelocityFromEntity.GetUpdater(owner.Target).Out(out var velocity);
				var controllerUpdater = impl.ControllerFromEntity.GetUpdater(owner.Target).Out(out var controller);
				controller.ControlOverVelocity.x = true;

				var throwOffset = new float3 {x = direction, y = 1.75f};
				var gravity     = new float3 {y = -25};

				ability.NextAttackDelay -= tick.Delta;

				if (ability.NextAttack > 0)
				{
					if (tick > UTick.AddMs(ability.NextAttack, YumiyachaBasicAttackAbility.ShootDelayMs))
					{
						ability.VolleyIndex++;
						ability.NextAttack = UTick.AddMs(tick, YumiyachaBasicAttackAbility.VolleyDelayMs);

						velocity.Value.x *= 0.5f;

						Debug.Log("shoot!");
					}

					if (tick < ability.NextAttack)
					{
						var targetPosition     = impl.LocalToWorldFromEntity[seekingState.Enemy].Position;
						var throwDeltaPosition = PredictTrajectory.Simple(throwOffset, new float3 {x = 15 * direction, y = 13}, gravity);
						targetPosition.x -= throwDeltaPosition.x;

						control.IsActive       = true;
						control.TargetPosition = targetPosition;
						control.Acceleration   = 5;
					}
					else
					{
						velocity.Value.x = math.lerp(velocity.Value.x, 0, math.clamp(playState.GetAcceleration() * 100 * tick.Delta, 0, 1));
						Debug.Log("slowdown!");
					}

					if (ability.VolleyIndex >= ability.VolleyCount)
					{
						ability.NextAttack      = UTick.CopyDelta(tick, 0);
						ability.NextAttackDelay = playState.AttackSpeed;
					}
				}
				else if ((state.Phase & EAbilityPhase.Chaining) != 0 || seekingState.Enemy == default)
					velocity.Value.x = math.lerp(velocity.Value.x, 0, playState.GetAcceleration() * 50 * tick.Delta);

				if ((state.Phase & EAbilityPhase.Active) != 0
				    && seekingState.Enemy != default)
				{
					var targetPosition     = impl.LocalToWorldFromEntity[seekingState.Enemy].Position;
					var throwDeltaPosition = PredictTrajectory.Simple(throwOffset, new float3 {x = 15 * direction, y = 13}, gravity);
					targetPosition.x -= throwDeltaPosition.x;

					control.IsActive       = true;
					control.TargetPosition = targetPosition;
					control.Acceleration   = 50;

					if (ability.NextAttack <= 0 && ability.NextAttackDelay <= 0)
					{
						ability.NextAttack  = tick;
						ability.VolleyIndex = 0;
					}
				}

				Debug.DrawRay(control.TargetPosition, Vector3.up, control.IsActive ? Color.cyan : (controller.ControlOverVelocity.x ? Color.black : Color.red), 0.1f);

				velocityUpdater.CompareAndUpdate(velocity);
				controllerUpdater.CompareAndUpdate(controller);
			}).Run();

			return default;
		}
	}
}