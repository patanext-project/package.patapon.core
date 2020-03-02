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
	public class ApplyEnergyFieldBuffRuleSystem : RuleBaseSystem
	{
		protected override void OnUpdate()
		{
			var relativeTeamFromEntity  = GetComponentDataFromEntity<Relative<TeamDescription>>(true);
			var buffContainerFromEntity = GetBufferFromEntity<BuffContainer>(true);
			var buffSourceFromEntity    = GetComponentDataFromEntity<BuffSource>(true);
			var energyFieldFromEntity   = GetComponentDataFromEntity<EnergyFieldBuff>(true);

			var ecb = new EntityCommandBuffer(Allocator.TempJob);

			Entities.ForEach((Entity ent, ref UnitPlayState playState, in Translation translation) =>
			        {
				        if (!relativeTeamFromEntity.TryGet(ent, out var relativeTeam))
					        return;

				        if (!buffContainerFromEntity.Exists(relativeTeam.Target))
					        return;

				        var highestDefensiveBonus      = 0.0f;
				        var lowestDamageReductionBonus = 1.0f;
				        var anyBuff                    = false;

				        var container = buffContainerFromEntity[relativeTeam.Target];
				        var positionX = translation.Value.x;
				        for (var i = 0; i != container.Length; i++)
				        {
					        // if this buff is from us or if it doesn't exist, continue...
					        if (buffSourceFromEntity[container[i].Target].Source == ent
					            || !energyFieldFromEntity.TryGet(container[i].Target, out var buff))
						        continue;

					        var sourceX = buff.Position.x;
					        var power   = 1f;
					        if ((positionX - sourceX) * buff.Direction > 0)
						        power = math.clamp(math.unlerp(buff.MaxDistance, buff.MinDistance, math.abs(positionX - sourceX)), 0, 1);

					        highestDefensiveBonus      = math.max(highestDefensiveBonus, math.lerp(0, buff.Defense, power));
					        lowestDamageReductionBonus = math.min(lowestDamageReductionBonus, math.lerp(1, buff.DamageReduction, power));

					        anyBuff |= power > 0;
				        }

				        if (anyBuff)
				        {
					        playState.Defense += Mathf.RoundToInt(highestDefensiveBonus);

					        var previousDmgReduction = playState.ReceiveDamagePercentage;
					        playState.ReceiveDamagePercentage *= playState.ReceiveDamagePercentage * lowestDamageReductionBonus;
					        if (playState.ReceiveDamagePercentage < 0.5f && previousDmgReduction > 0.5f)
						        playState.ReceiveDamagePercentage = 0.5f;

					        ecb.AddComponent<EnergyFieldBuff.HasBonusTag>(ent);
				        }
				        else
					        ecb.RemoveComponent<EnergyFieldBuff.HasBonusTag>(ent);
			        })
			        .WithReadOnly(relativeTeamFromEntity)
			        .WithReadOnly(buffContainerFromEntity)
			        .WithReadOnly(energyFieldFromEntity)
			        .Run();

			ecb.Playback(EntityManager);
			ecb.Dispose();
		}
	}
}