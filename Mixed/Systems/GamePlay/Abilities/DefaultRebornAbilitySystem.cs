using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(RhythmAbilitySystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	[AlwaysSynchronizeSystem]
	public class DefaultRebornAbilitySystem : JobComponentSystem
	{
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var engineStateFromEntity = GetComponentDataFromEntity<RhythmEngineState>(true);
			var comboStateFromEntity  = GetComponentDataFromEntity<GameComboState>();
			var healthFromEntity      = GetComponentDataFromEntity<LivableHealth>(true);

			var eventQueue = World.GetExistingSystem<TargetRebornEvent.Provider>()
			                      .GetEntityDelayedStream();

			Entities
				.ForEach((ref DefaultRebornAbility ability, ref AbilityRhythmState state, ref Owner owner) =>
				{
					if (!ability.WasFever && !state.PreviousActiveCombo.CanSummon
					    || state.Engine == default
					    || !healthFromEntity[owner.Target].IsDead)
					{
						ability.WasFever = false;
						return;
					}

					var engineState = engineStateFromEntity[state.Engine];
					//Debug.Log($"{ability.LastPressureBeat} {engineState.LastPressureBeat + 1}");
					if (ability.LastPressureBeat + 1 < engineState.LastPressureBeat)
					{
						state.PreviousActiveCombo = default;
						ability.WasFever          = false;
					}
					else if (state.PreviousActiveCombo.CanSummon)
					{
						ability.WasFever = true;
					}

					ability.LastPressureBeat = engineState.LastPressureBeat;

					if (ability.WasFever && state.IsActive)
					{
						var comboUpdater = comboStateFromEntity.GetUpdater(state.Engine)
						                                       .Out(out var comboState);

						var max = comboState.JinnEnergyMax;
						comboState               = default;
						comboState.JinnEnergyMax = max;
						comboUpdater.Update(comboState);

						eventQueue.Enqueue(new TargetRebornEvent
						{
							Target = owner.Target
						});

						ability.WasFever = false;
					}
				})
				.WithReadOnly(engineStateFromEntity)
				.WithReadOnly(healthFromEntity)
				.Run();

			return inputDeps;
		}
	}

	public struct TargetRebornEvent : IComponentData
	{
		public Entity Target;

		[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities.SpawnEvent))]
		public class Provider : BaseProviderBatch<TargetRebornEvent>
		{
			private EntityQuery m_Query;

			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(GameEvent),
					typeof(TargetRebornEvent),
					typeof(GhostEntity)
				};
			}

			public override void SetEntityData(Entity entity, TargetRebornEvent data)
			{
				EntityManager.SetComponentData(entity, data);
			}

			protected override void OnUpdate()
			{
				m_Query = m_Query ?? GetEntityQuery(typeof(GameEvent), typeof(TargetRebornEvent));
				EntityManager.DestroyEntity(m_Query);

				base.OnUpdate();
			}
		}
	}
}