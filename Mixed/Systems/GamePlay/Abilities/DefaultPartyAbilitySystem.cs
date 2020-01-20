using System;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(RhythmAbilitySystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class DefaultPartyAbilitySystem : JobGameBaseSystem
	{
		private LazySystem<TargetDamageEvent.Provider> m_EventProvider;
		
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var tick                 = ServerTick;
			var comboStateFromEntity = GetComponentDataFromEntity<GameComboState>();

			var livableHealthFromEntity = GetComponentDataFromEntity<LivableHealth>(true);

			var engineProcessFromEntity = GetComponentDataFromEntity<RhythmEngineState>(true);
			var ecb = this.L(ref m_EventProvider).CreateEntityCommandBuffer().ToConcurrent();
			var evArchetype = m_EventProvider.Value.EntityArchetype;
			
			var impl = new BasicUnitAbilityImplementation(this);
			var seekingStateFromEntity = GetComponentDataFromEntity<UnitEnemySeekingState>(true);
			
			var rand = new Random((uint) Environment.TickCount);

			inputDeps =
				Entities
					.ForEach((Entity entity, int nativeThreadIndex, ref RhythmAbilityState state, ref DefaultPartyAbility partyAbility, in Owner owner) =>
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

						if (livableHealthFromEntity.TryGet(owner.Target, out var health) && !health.IsDead
						    && engineProcessFromEntity[state.Engine].IsNewBeat)
						{
							var gainHealth = 4;
							if (seekingStateFromEntity.TryGet(owner.Target, out var seeking) && seeking.Enemy == default)
								gainHealth += 4;
							
							var position = impl.LocalToWorldFromEntity[owner.Target].Position;
							var evEnt = ecb.CreateEntity(nativeThreadIndex, evArchetype);
							ecb.AddComponent(nativeThreadIndex, evEnt, new TargetDamageEvent {Destination = owner.Target, Origin = owner.Target, Damage = gainHealth});
							ecb.AddComponent(nativeThreadIndex, evEnt, new Translation
							{
								Value = new float3(position.x + math.lerp(-0.2f, 0.2f, rand.NextFloat()), position.y + math.lerp(0.8f, 1.2f, rand.NextFloat()), 0)
							});
						}

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
					.WithNativeDisableParallelForRestriction(engineProcessFromEntity)
					.WithReadOnly(livableHealthFromEntity)
					.WithReadOnly(seekingStateFromEntity)
					.Schedule(inputDeps);
			
			m_EventProvider.Value.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}