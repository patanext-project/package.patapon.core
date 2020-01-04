using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.RhythmEngine;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(ActionSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class DefaultPartyAbilitySystem : JobGameBaseSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var tick                 = ServerTick;
			var comboStateFromEntity = GetComponentDataFromEntity<GameComboState>();

			inputDeps =
				Entities
					.ForEach((Entity entity, ref RhythmAbilityState state, ref DefaultPartyAbility partyAbility, in Owner owner) =>
					{
						if (!state.IsActive)
						{
							partyAbility.WasActive = false;
							partyAbility.Progression.Reset();
							return;
						}

						var isActivationFrame = false;
						if (!partyAbility.WasActive)
							isActivationFrame = partyAbility.WasActive = true;

						if (state.Combo.IsFever)
						{
							partyAbility.Progression += tick;
							if (partyAbility.Progression.Value > 0)
							{
								var energy = partyAbility.Progression.Value / partyAbility.TickPerSecond;
								if (energy > 0)
								{
									partyAbility.Progression.Value = 0;

									var combo = comboStateFromEntity[state.Engine];
									combo.JinnEnergy                   += energy * partyAbility.EnergyPerTick;
									comboStateFromEntity[state.Engine] =  combo;
								}
							}

							if (isActivationFrame)
							{
								var combo = comboStateFromEntity[state.Engine];
								combo.JinnEnergy                   += partyAbility.EnergyOnActivation;
								comboStateFromEntity[state.Engine] =  combo;
							}
						}
						else
						{
							partyAbility.Progression.Reset();
						}
					})
					.WithNativeDisableParallelForRestriction(comboStateFromEntity)
					.Schedule(inputDeps);

			return inputDeps;
		}
	}
}