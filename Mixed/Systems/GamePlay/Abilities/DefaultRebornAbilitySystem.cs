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
using UnityEngine;

namespace Systems.GamePlay
{
	[UpdateInGroup(typeof(RhythmAbilitySystemGroup))]
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
				.ForEach((ref DefaultRebornAbility ability, ref RhythmAbilityState state, ref Owner owner) =>
				{
					if ((!ability.WasFever && !state.PreviousActiveCombo.IsFever)
					    || state.Engine == default
					    || !healthFromEntity[owner.Target].IsDead)
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

			private EntityQuery m_Query;

			protected override void OnUpdate()
			{
				m_Query = m_Query ?? GetEntityQuery(typeof(GameEvent), typeof(TargetImpulseEvent));
				EntityManager.DestroyEntity(m_Query);

				base.OnUpdate();
			}
		}
	}
}