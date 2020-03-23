using Systems.GamePlay.CYari;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.Abilities.CTate;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Systems.GamePlay.CTate
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.ClientAndServer)]
	public unsafe class TaterazayEnergyFieldAbilitySystem : BaseAbilitySystem
	{
		protected override void OnUpdate()
		{
			var tick                     = ServerTick;
			var impl                     = new BasicUnitAbilityImplementation(this);
			var marchCommandFromEntity   = GetComponentDataFromEntity<MarchCommand>(true);
			var relativeTeamFromEntity   = GetComponentDataFromEntity<Relative<TeamDescription>>(true);
			var relativeTargetFromEntity = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);

			var buffFromEntity          = GetComponentDataFromEntity<EnergyFieldBuff>();
			var buffSourceFromEntity    = GetComponentDataFromEntity<BuffSource>();
			var buffForTargetFromEntity = GetComponentDataFromEntity<BuffForTarget>();

			var isPredicted = World.GetExistingSystem<RhythmAbilitySystemGroup>().IsPredicted;
			var ecb         = new EntityCommandBuffer(Allocator.TempJob);

			Entities
				.ForEach((Entity          entity, int                 nativeThreadIndex, ref TaterazayEnergyFieldAbility ability, ref DefaultSubsetMarch subSetMarch, ref AbilityControlVelocity control,
				          in AbilityState state,  in AbilityEngineSet engineSet,         in  Owner                       owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target) || (state.Phase & EAbilityPhase.ActiveOrChaining) == 0)
					{
						// disable buff
						ecb.AddComponent<Disabled>(ability.BuffEntity);
						return;
					}

					subSetMarch.IsActive = (state.Phase & EAbilityPhase.Active) != 0 && marchCommandFromEntity.Exists(engineSet.Command);

					var tryGetChain = stackalloc[] {entity, owner.Target};
					if (relativeTargetFromEntity.TryGetChain(tryGetChain, 2, out var relativeTarget))
					{
						var targetPosition = impl.Translation[relativeTarget.Target].Value;
						if ((state.Phase & EAbilityPhase.ActiveOrChaining) != 0 && isPredicted)
						{
							control.IsActive       = true;
							control.TargetPosition = targetPosition;
							control.Acceleration   = 25;
						}
					}

					// re-enable buff
					ecb.RemoveComponent<Disabled>(ability.BuffEntity);

					SetComponent(ability.BuffEntity, new EnergyFieldBuff
					{
						DamageReduction = ability.GivenDamageReduction,
						Defense = GetComponent<UnitPlayState>(owner.Target).Defense
					});
					SetComponent(ability.BuffEntity, new Translation {Value = GetComponent<Translation>(owner.Target).Value});
					SetComponent(ability.BuffEntity, new UnitDirection {Value = GetComponent<UnitDirection>(owner.Target).Value});
					buffSourceFromEntity[ability.BuffEntity] = new BuffSource {Source = owner.Target};
					if (relativeTeamFromEntity.TryGet(owner.Target, out var relativeTeam))
						buffForTargetFromEntity[ability.BuffEntity] = new BuffForTarget {Target = relativeTeam.Target};
				})
				.WithReadOnly(relativeTargetFromEntity)
				.WithReadOnly(relativeTeamFromEntity)
				.WithReadOnly(marchCommandFromEntity)
				.Run();

			ecb.Playback(EntityManager);
			ecb.Dispose();
		}
	}
}