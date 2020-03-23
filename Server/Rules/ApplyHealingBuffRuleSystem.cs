using Systems.GamePlay.CPingrek;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.Abilities.CTate;
using Patapon.Mixed.Units;
using Rules;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Patapon.Mixed.GameModes.Rules
{
	[AlwaysSynchronizeSystem]
	[UpdateInGroup(typeof(GameEventRuleSystemGroup))]
	[UpdateBefore(typeof(ApplyDefensiveBonusToDamageRuleSystem))]
	public class ApplyHealingBuffRuleSystem : RuleBaseSystem
	{
		private LazySystem<TargetDamageEvent.Provider> m_EventProvider;

		protected override void OnUpdate()
		{
			var relativeTeamFromEntity  = GetComponentDataFromEntity<Relative<TeamDescription>>(true);
			var buffContainerFromEntity = GetBufferFromEntity<BuffContainer>(true);
			var buffSourceFromEntity    = GetComponentDataFromEntity<BuffSource>(true);
			var energyFieldFromEntity   = GetComponentDataFromEntity<HealingBuff>(true);

			var damageArchetype = this.L(ref m_EventProvider).EntityArchetype;
			var ecb             = m_EventProvider.Value.CreateEntityCommandBuffer();

			Entities.ForEach((Entity ent, ref UnitPlayState playState, in Translation translation) =>
			        {
				        if (!relativeTeamFromEntity.TryGet(ent, out var relativeTeam))
					        return;
				        
				        if (!buffContainerFromEntity.Exists(relativeTeam.Target))
					        return;
				        
				        var highestHealing = 0.0f;
				        var anyBuff        = false;

				        var container = buffContainerFromEntity[relativeTeam.Target];
				        var positionX = translation.Value.x;
				        for (var i = 0; i != container.Length; i++)
				        {
					        var buffEntity = container[i].Target;

					        // if this buff is from us or if it doesn't exist, continue...
					        if (buffSourceFromEntity[buffEntity].Source == ent
					            || !energyFieldFromEntity.TryGet(buffEntity, out var buff))
						        continue;

					        var sourceX  = GetComponent<Translation>(buffEntity).Value.x;
					        var distance = GetComponent<BuffDistance>(buffEntity);
					        var power    = 1f;
					        if ((positionX - sourceX) * GetComponent<UnitDirection>(buffEntity).Value > 0)
						        power = math.clamp(math.unlerp(distance.Max, distance.Min, math.abs(positionX - sourceX)), 0, 1);

					        highestHealing =  math.max(highestHealing, math.lerp(0, buff.Value, power));
					        anyBuff        |= power > 0;
				        }
				        
				        if (anyBuff)
				        {
					        var ev = ecb.CreateEntity(damageArchetype);
					        ecb.SetComponent(ev, new TargetDamageEvent
					        {
						        Origin      = default,
						        Destination = ent,
						        Damage      = (int) math.round(highestHealing)
					        });
					        ecb.AddComponent(ev, new Translation {Value = translation.Value + new float3(0, 1, 0)});
				        }
			        })
			        .WithReadOnly(relativeTeamFromEntity)
			        .WithReadOnly(buffContainerFromEntity)
			        .WithReadOnly(energyFieldFromEntity)
			        .Run();
		}
	}
}