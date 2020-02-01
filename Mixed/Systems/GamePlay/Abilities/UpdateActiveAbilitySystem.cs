using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Systems.GamePlay
{
	[AlwaysSynchronizeSystem]
	[UpdateBefore(typeof(ActionSystemGroup))]
	[UpdateBefore(typeof(UnitInitStateSystemGroup))]
	public class UpdateActiveAbilitySystem : JobComponentSystem
	{
		public static bool IsComboIdentical(FixedList32<Entity> abilityCombo, FixedList32<Entity> unitCombo)
		{
			var start = unitCombo.Length - 1 - abilityCombo.Length;
			var end   = unitCombo.Length - 1;

			if ((end - start) < abilityCombo.Length || start < 0)
				return false;

			for (var i = start; i != end; i++)
			{
				if (abilityCombo[i - start] != unitCombo[i])
					return false;
			}

			return true;
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var engineRelativeFromEntity       = GetComponentDataFromEntity<Relative<RhythmEngineDescription>>(true);
			var rhythmEngineProcessFromEntity  = GetComponentDataFromEntity<FlowEngineProcess>(true);
			var rhythmEngineSettingsFromEntity = GetComponentDataFromEntity<RhythmEngineSettings>(true);
			var rhythmEngineStateFromEntity    = GetComponentDataFromEntity<RhythmEngineState>(true);
			var rhythmCurrentCommandFromEntity = GetComponentDataFromEntity<RhythmCurrentCommand>(true);
			var gameCommandStateFromEntity     = GetComponentDataFromEntity<GameCommandState>(true);
			var gameComboStateFromEntity       = GetComponentDataFromEntity<GameComboState>(true);

			var controllerFromEntity        = GetComponentDataFromEntity<AbilityState>();
			var activationCommandFromEntity = GetComponentDataFromEntity<AbilityActivation>(true);
			var abilityEngineFromEntity     = GetComponentDataFromEntity<AbilityEngineSet>();

			Entities.ForEach((Entity ent, ref OwnerActiveAbility activeSelf, in DynamicBuffer<ActionContainer> container) =>
			{
				if (!engineRelativeFromEntity.Exists(ent))
					return;

				var engineEntity   = engineRelativeFromEntity[ent].Target;
				var engineProcess  = rhythmEngineProcessFromEntity[engineEntity];
				var engineSettings = rhythmEngineSettingsFromEntity[engineEntity];
				var currentCommand = rhythmCurrentCommandFromEntity[engineEntity];
				var gameCombo      = gameComboStateFromEntity[engineEntity];
				var gameCommand    = gameCommandStateFromEntity[engineEntity];

				if (gameCommand.StartTime == -1 && gameCommand.EndTime == -1)
					currentCommand.CommandTarget = default;

				var isNewIncomingCommand = false;
				// we are not doing any sort of combo, let's clear things...
				if (gameCombo.Chain <= 0)
				{
					activeSelf.LastActivationTime = -1;
					activeSelf.CurrentCombo.Clear();
				}
				// New command detected! let's add it to the  C O M B O  list.
				else if (activeSelf.LastCommandActiveTime != gameCommand.StartTime
				         && currentCommand.CommandTarget != default)
				{
					activeSelf.LastCommandActiveTime = gameCommand.StartTime;
					activeSelf.LastActivationTime = -1;
					activeSelf.AddCombo(currentCommand.CommandTarget);

					isNewIncomingCommand = true;
				}

				activeSelf.Incoming = default;

				// Select correct abilities, and update state of owned abilities...
				var length = container.Length;
				var priority = -1;
				for (var i = 0; i != length; i++)
				{
					var actionEntity = container[i].Target;
					if (!controllerFromEntity.Exists(actionEntity))
						continue;
					
					var activation   = activationCommandFromEntity[actionEntity];
					var commandPriority = activation.Combos.Length;

					if (activation.Chaining == currentCommand.CommandTarget
					    && IsComboIdentical(activation.Combos, activeSelf.CurrentCombo)
					    && (activeSelf.Incoming == default || activeSelf.Incoming != default && priority < commandPriority)
					    && (activation.Selection == gameCommand.Selection
					        || activeSelf.Incoming == default && activation.Selection == AbilitySelection.Horizontal))
					{
						if (activation.Selection == gameCommand.Selection)
							priority = commandPriority;
						
						activeSelf.Incoming = actionEntity;
					}

					abilityEngineFromEntity[actionEntity] = new AbilityEngineSet
					{
						Engine          = engineEntity,
						Process         = engineProcess,
						Settings        = engineSettings,
						CurrentCommand  = currentCommand,
						ComboState      = gameCombo,
						CommandState    = gameCommand,
						Command         = currentCommand.CommandTarget,
						PreviousCommand = currentCommand.Previous,
						Combo           = gameCombo,
						PreviousCombo   = default
					};

					var controller = controllerFromEntity[actionEntity];
					controller.Phase = EAbilityPhase.None;

					controllerFromEntity[actionEntity] = controller;
				}

				// If we are not chaining anymore or if the chain is finished, terminate our current command.
				if ((!gameCommand.HasActivity(engineProcess.Milliseconds, engineSettings.BeatInterval) && gameCommand.ChainEndTime < engineProcess.Milliseconds || gameCombo.Chain <= 0)
				    && activeSelf.Active != default)
				{
					var controller = controllerFromEntity[activeSelf.Active];
					controller.Phase = EAbilityPhase.None;

					controllerFromEntity[activeSelf.Active] = controller;

					activeSelf.Active = default;
				}

				if (gameCommand.StartTime <= engineProcess.Milliseconds
				    && activeSelf.Active != activeSelf.Incoming)
				{
					activeSelf.Active = activeSelf.Incoming;
				}

				// We update incoming state before active state (in case if it's the same ability...)
				if (activeSelf.Incoming != default)
				{
					var incomingController = controllerFromEntity[activeSelf.Incoming];
					incomingController.Phase |= EAbilityPhase.WillBeActive;
					if (isNewIncomingCommand)
						incomingController.UpdateVersion++;

					controllerFromEntity[activeSelf.Incoming] = incomingController;
				}

				if (activeSelf.Active != default)
				{
					var activeController = controllerFromEntity[activeSelf.Active];
					if (gameCommand.StartTime <= engineProcess.Milliseconds)
						activeController.Phase = EAbilityPhase.None;
					
					activeController.Phase |= gameCommand.IsGamePlayActive(engineProcess.Milliseconds) ? EAbilityPhase.Active : EAbilityPhase.Chaining;
					if (gameCommand.StartTime <= engineProcess.Milliseconds && activeSelf.LastActivationTime == -1)
					{
						activeSelf.LastActivationTime = engineProcess.Milliseconds;
						if (activeController.ActivationVersion == 0)
							activeController.ActivationVersion++;
						
						activeController.ActivationVersion++;
					}

					controllerFromEntity[activeSelf.Active] = activeController;
				}
			}).Run();

			return default;
		}
	}
}