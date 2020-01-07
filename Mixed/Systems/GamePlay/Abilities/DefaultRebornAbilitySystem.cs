using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Systems.GamePlay
{
	[AlwaysSynchronizeSystem]
	public class DefaultRebornAbilitySystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var engineStateFromEntity = GetComponentDataFromEntity<RhythmEngineState>(true);
			var comboStateFromEntity  = GetComponentDataFromEntity<GameComboState>();

			Entities
				.ForEach((ref DefaultRebornAbility ability, ref RhythmAbilityState state) =>
				{
					if ((!ability.WasFever && !state.PreviousActiveCombo.IsFever)
					    || state.Engine == default)
					{
						ability.WasFever = false;
						return;
					}

					var engineState = engineStateFromEntity[state.Engine];
					if (!(ability.LastPressureBeat <= engineState.LastPressureBeat + 1))
					{
						state.PreviousActiveCombo = default;
						ability.WasFever          = false;
					}
					else if (state.PreviousActiveCombo.IsFever)
					{
						ability.WasFever = true;
					}

					ability.LastPressureBeat = engineState.LastPressureBeat;

					if (ability.WasFever && state.IsActive)
					{
						var comboUpdater = comboStateFromEntity.GetUpdater(state.Engine)
						                                       .Out(out var comboState);
						comboState = default;
						comboUpdater.Update(comboState);

						Debug.Log("reborn!");

						ability.WasFever = false;
					}
				})
				.WithReadOnly(engineStateFromEntity)
				.Run();

			return inputDeps;
		}
	}
}