using System;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(RhythmAbilitySystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[AlwaysSynchronizeSystem]
	public class DefaultPartyAbilitySystem : JobGameBaseSystem
	{
		private LazySystem<TargetDamageEvent.Provider> m_EventProvider;

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var tick                 = ServerTick;
			var comboStateFromEntity = GetComponentDataFromEntity<GameComboState>();

			var livableHealthFromEntity = GetComponentDataFromEntity<LivableHealth>(true);

			var engineProcessFromEntity = GetComponentDataFromEntity<RhythmEngineState>(true);
			var ecb                     = this.L(ref m_EventProvider).CreateEntityCommandBuffer();
			var evArchetype             = m_EventProvider.Value.EntityArchetype;

			var impl                   = new BasicUnitAbilityImplementation(this);
			var seekingStateFromEntity = GetComponentDataFromEntity<UnitEnemySeekingState>(true);

			var rand = new Random((uint) Environment.TickCount);

			Entities
				.ForEach((Entity entity, int nativeThreadIndex, ref DefaultPartyAbility partyAbility, in AbilityState controller, in AbilityEngineSet engineSet, in Owner owner) =>
				{
					if (controller.Phase == EAbilityPhase.None)
					{
						partyAbility.WasActive = false;
						partyAbility.Progression.Reset();
						return;
					}

					var isActivationFrame = false;
					if (!partyAbility.WasActive)
						isActivationFrame = partyAbility.WasActive = true;

					if (livableHealthFromEntity.TryGet(owner.Target, out var health) && !health.IsDead
					                                                                 && engineProcessFromEntity[engineSet.Engine].IsNewBeat)
					{
						var gainHealth = 4;
						if (seekingStateFromEntity.TryGet(owner.Target, out var seeking) && seeking.Enemy == default)
							gainHealth += 4;

						var position = impl.LocalToWorldFromEntity[owner.Target].Position;
						var evEnt    = ecb.CreateEntity(evArchetype);
						ecb.AddComponent(evEnt, new TargetDamageEvent {Destination = owner.Target, Origin = owner.Target, Damage = gainHealth});
						ecb.AddComponent(evEnt, new Translation
						{
							Value = new float3(position.x + math.lerp(-0.4f, 0.25f, rand.NextFloat()), position.y + math.lerp(0.8f, 1.2f, rand.NextFloat()), 0)
						});
					}

					if (engineSet.Combo.IsFever)
					{
						partyAbility.Progression += tick;
						if (partyAbility.Progression.Value > 0)
						{
							var energy = partyAbility.Progression.Value / partyAbility.TickPerSecond;
							if (energy > 0)
							{
								partyAbility.Progression.Value = 0;

								var combo = comboStateFromEntity[engineSet.Engine];
								combo.JinnEnergy                       += energy * partyAbility.EnergyPerTick;
								comboStateFromEntity[engineSet.Engine] =  combo;
							}
						}

						if (isActivationFrame)
						{
							var combo = comboStateFromEntity[engineSet.Engine];
							combo.JinnEnergy                       += partyAbility.EnergyOnActivation;
							comboStateFromEntity[engineSet.Engine] =  combo;
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
				.Run();

			return default;
		}
	}
}