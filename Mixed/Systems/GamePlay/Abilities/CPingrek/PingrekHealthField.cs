using System;
using Systems.GamePlay.CYari;
using P4TLB.MasterServer;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems.GamePlay.CPingrek
{
	public struct HealingBuff : IComponentData
	{
		public int Value;
	}

	public struct PingrekHealthField : IComponentData
	{
		public Entity BuffEntity;
		public float  AttackDelay;

		public class Provider : BaseRhythmAbilityProvider<PingrekHealthField>
		{
			public override string MasterServerId  => nameof(P4OfficialAbilities.MahosuHealthField);
			public override Type   ChainingCommand => typeof(DefendCommand);
			protected override string file_path_prefix => "maho";

			public override void SetEntityData(Entity entity, CreateAbility data)
			{
				base.SetEntityData(entity, data);
				// for later lul
				
				var buff = EntityManager.CreateEntity(typeof(BuffModifierDescription), typeof(Translation), typeof(UnitDirection), typeof(BuffDistance), typeof(BuffForTarget), typeof(BuffSource), typeof(HealingBuff));
				EntityManager.ReplaceOwnerData(buff, entity);
				EntityManager.SetEnabled(buff, false);
				
				SetComponent(entity, new PingrekHealthField
				{
					BuffEntity = buff
				});
			}
		}

		public class System : BaseAbilitySystem
		{
			protected override void OnUpdate()
			{
				var impl = new BasicUnitAbilityImplementation(this);
				var dt   = Time.DeltaTime;

				var ecb = new EntityCommandBuffer(Allocator.TempJob);

				Entities.ForEach((ref PingrekHealthField ability, ref AbilityControlVelocity control, in AbilityState state, in AbilityEngineSet engineSet, in Owner owner) =>
				{
					ecb.AddComponent<Disabled>(ability.BuffEntity);

					if (!impl.CanExecuteAbility(owner.Target))
						return;

					ability.AttackDelay -= dt;
					
					if (state.IsActive)
					{
						control.IsActive         = true;
						control.TargetFromCursor = true;
						if (ability.AttackDelay <= 0)
						{
							var playState = GetComponent<UnitPlayState>(owner.Target);
							ability.AttackDelay = playState.AttackSpeed;

							// re-enable buff
							ecb.RemoveComponent<Disabled>(ability.BuffEntity);

							SetComponent(ability.BuffEntity, new HealingBuff {Value   = playState.Attack});
							SetComponent(ability.BuffEntity, new Translation {Value   = GetComponent<Translation>(owner.Target).Value});
							SetComponent(ability.BuffEntity, new UnitDirection {Value = GetComponent<UnitDirection>(owner.Target).Value});
							SetComponent(ability.BuffEntity, new BuffSource {Source   = owner.Target});
							if (HasComponent<Relative<TeamDescription>>(owner.Target))
								SetComponent(ability.BuffEntity, new BuffForTarget {Target = GetComponent<Relative<TeamDescription>>(owner.Target).Target});
						}
					}
				}).Run();
				
				ecb.Playback(EntityManager);
				ecb.Dispose();
			}
		}
	}
}