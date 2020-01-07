using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.RhythmEngine;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Systems.GamePlay
{
	public class DefaultRebornAbilitySystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var engineStateFromEntity = GetComponentDataFromEntity<RhythmEngineState>(true);

			inputDeps
				= Entities
				  .ForEach((ref DefaultRebornAbility ability, ref RhythmAbilityState state) =>
				  {
					  if (!state.PreviousActiveCombo.IsFever
					      || state.Engine == default)
					  {
						  ability.WasFever = false;
						  return;
					  }

					  ability.WasFever = true;
					  var engineState = engineStateFromEntity[state.Engine];
					  if (!(ability.LastPressureBeat <= engineState.LastPressureBeat + 1))
					  {
						  state.PreviousActiveCombo = default;
					  }

					  ability.LastPressureBeat = engineState.LastPressureBeat;

					  if (state.PreviousActiveCombo.IsFever && state.IsActive)
					  {
						  Debug.Log("reborn!");
					  }
				  })
				  .WithReadOnly(engineStateFromEntity)
				  .Schedule(inputDeps);

			return inputDeps;
		}
	}
}