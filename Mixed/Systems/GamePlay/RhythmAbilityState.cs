using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace Patapon.Mixed.GamePlay
{
	[UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
	[UpdateBefore(typeof(ActionSystemGroup))]
	[UpdateBefore(typeof(UnitInitStateSystemGroup))]
	public class UpdateRhythmAbilityState : JobComponentSystem
	{
		[BurstDiscard]
		private static void NonBurst_ErrorNoOwnerOrNoRelative(Entity owner, Entity entity)
		{
			if (owner == default)
				Debug.LogError("Default owner found on " + entity);
		}

		[BurstDiscard]
		private static void NonBurst_ErrorNoRhythmEngine(Entity target, Entity entity)
		{
			Debug.LogError($"No RhythmEngine found on target({target}) of ability({entity})");
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var relativeRhythmEngineFromEntity    = GetComponentDataFromEntity<Relative<RhythmEngineDescription>>(true);
			var rhythmEngineDescriptionFromEntity = GetComponentDataFromEntity<RhythmEngineDescription>(true);
			var rhythmEngineProcessFromEntity     = GetComponentDataFromEntity<FlowEngineProcess>(true);
			var rhythmEngineStateFromEntity       = GetComponentDataFromEntity<RhythmEngineState>(true);
			var rhythmCurrentCommandFromEntity    = GetComponentDataFromEntity<RhythmCurrentCommand>(true);
			var gameCommandStateFromEntity        = GetComponentDataFromEntity<GameCommandState>(true);
			var gameComboStateFromEntity          = GetComponentDataFromEntity<GameComboState>(true);

			var actionContainer = GetBufferFromEntity<ActionContainer>(true);

			var abilityStateFromEntity = GetComponentDataFromEntity<RhythmAbilityState>(true);

			return Entities
			       .ForEach((Entity entity, ref RhythmAbilityState abilityState, in Owner owner) =>
			       {
				       if (owner.Target == default || !relativeRhythmEngineFromEntity.Exists(owner.Target))
				       {
					       NonBurst_ErrorNoOwnerOrNoRelative(owner.Target, entity);
					       return;
				       }

				       var engine = relativeRhythmEngineFromEntity[owner.Target].Target;
				       if (!rhythmEngineDescriptionFromEntity.Exists(engine))
				       {
					       NonBurst_ErrorNoRhythmEngine(engine, entity);
					       return;
				       }

				       if (!actionContainer.Exists(owner.Target))
					       return;

				       var forceSelectionActive = false;
				       // try to check if the selection can still be used on us.
				       // this will only work if there is an empty ability in the target selection
				       // or if the targeted ability is currently in wait mode...
				       if (rhythmCurrentCommandFromEntity[engine].CommandTarget == abilityState.Command
				           && abilityState.TargetSelection != gameCommandStateFromEntity[engine].Selection)
				       {
					       var actionBuffer    = actionContainer[owner.Target];
					       var foundTarget     = false;
					       var engineSelection = gameCommandStateFromEntity[engine].Selection;
					       for (var i = 0; i != actionBuffer.Length; i++)
					       {
						       var action      = actionBuffer[i].Target;
						       var actionState = abilityStateFromEntity[action];
						       if (actionState.Command != abilityState.Command)
							       continue;

						       if (abilityStateFromEntity[action].TargetSelection == engineSelection)
						       {
							       foundTarget = true;
							       break;
						       }

						       // todo: check if ability is in cooldown...
					       }

					       if (!foundTarget)
					       {
						       forceSelectionActive = true;
					       }
				       }

				       abilityState.Engine = engine;
				       abilityState.Calculate(rhythmCurrentCommandFromEntity[engine],
					       gameCommandStateFromEntity[engine],
					       gameComboStateFromEntity[engine],
					       rhythmEngineProcessFromEntity[engine],
					       rhythmEngineStateFromEntity[engine], forceSelectionActive);
			       })
			       .WithReadOnly(actionContainer)
			       .WithReadOnly(relativeRhythmEngineFromEntity)
			       .WithReadOnly(rhythmEngineDescriptionFromEntity)
			       .WithReadOnly(rhythmEngineProcessFromEntity)
			       .WithReadOnly(rhythmEngineStateFromEntity)
			       .WithReadOnly(rhythmCurrentCommandFromEntity)
			       .WithReadOnly(gameCommandStateFromEntity)
			       .WithReadOnly(gameComboStateFromEntity)
			       .WithNativeDisableContainerSafetyRestriction(abilityStateFromEntity) // aliasing
			       .Schedule(inputDeps);
		}
	}
}