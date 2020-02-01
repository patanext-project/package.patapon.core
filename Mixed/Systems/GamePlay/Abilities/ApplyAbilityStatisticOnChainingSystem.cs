using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(RhythmAbilitySystemGroup))]
	public class ApplyAbilityStatisticOnChainingSystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var playStateFromEntity     = GetComponentDataFromEntity<UnitPlayState>();
			var chargeCommandFromEntity = GetComponentDataFromEntity<ChargeCommand>(true);

			return Entities.ForEach((in AbilityState state, in AbilityModifyStatsOnChaining modify, in AbilityEngineSet engineSet, in Owner owner) =>
			               {
				               if ((state.Phase & EAbilityPhase.ActiveOrChaining) == 0)
					               return;

				               var playState  = playStateFromEntity[owner.Target];
				               var hasCharged = chargeCommandFromEntity.Exists(engineSet.PreviousCommand);

				               if (hasCharged && modify.SetChargeModifierAsFirst)
					               modify.ChargeModifier.Multiply(ref playState);

				               playState = AbilityUtility.CompileStat(engineSet.Combo, playState, modify.ActiveModifier, modify.FeverModifier, modify.PerfectModifier);

				               if (hasCharged && !modify.SetChargeModifierAsFirst)
					               modify.ChargeModifier.Multiply(ref playState);

				               playStateFromEntity[owner.Target] = playState;
			               })
			               .WithNativeDisableParallelForRestriction(playStateFromEntity)
			               .WithReadOnly(chargeCommandFromEntity)
			               .Schedule(inputDeps);
		}
	}
}