using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Systems;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace Systems.GamePlay
{
	[AlwaysSynchronizeSystem]
	[UpdateAfter(typeof(RhythmEngineGroup))]
	[UpdateBefore(typeof(ActionSystemGroup))]
	[UpdateBefore(typeof(UnitInitStateSystemGroup))]
	public class UpdateActiveAbilitySystem : AbsGameBaseSystem
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

		protected override void OnUpdate()
		{
			var engineRelativeFromEntity       = GetComponentDataFromEntity<Relative<RhythmEngineDescription>>(true);
			var rhythmEngineProcessFromEntity  = GetComponentDataFromEntity<FlowEngineProcess>(true);
			var rhythmEngineSettingsFromEntity = GetComponentDataFromEntity<RhythmEngineSettings>(true);
			var rhythmHeroStateFromEntity      = GetComponentDataFromEntity<RhythmHeroState>();
			var rhythmCurrentCommandFromEntity = GetComponentDataFromEntity<RhythmCurrentCommand>(true);
			var gameCommandStateFromEntity     = GetComponentDataFromEntity<GameCommandState>(true);
			var gameComboStateFromEntity       = GetComponentDataFromEntity<GameComboState>();

			var predictedGameComboStateFromEntity = GetComponentDataFromEntity<GameComboPredictedClient>(true);
			var predictedGameCommandFromEntity = GetComponentDataFromEntity<GamePredictedCommandState>(true);
				
			var controllerFromEntity        = GetComponentDataFromEntity<AbilityState>();
			var activationCommandFromEntity = GetComponentDataFromEntity<AbilityActivation>(true);
			var abilityEngineFromEntity     = GetComponentDataFromEntity<AbilityEngineSet>();

			var isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
			var tick     = ServerTick;

			var simulatedFromEntity = GetComponentDataFromEntity<FlowSimulateProcess>(true);
			
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

				if (simulatedFromEntity.Exists(engineEntity))
				{
					if (predictedGameComboStateFromEntity.TryGet(engineEntity, out var predictedGameCombo))
						gameCombo = predictedGameCombo.State;
					if (predictedGameCommandFromEntity.TryGet(engineEntity, out var predictedCommand))
						gameCommand = predictedCommand.State;
				}

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
				else if (activeSelf.LastCommandActiveTime != currentCommand.ActiveAtTime
				         && currentCommand.CommandTarget != default)
				{
					activeSelf.LastCommandActiveTime = currentCommand.ActiveAtTime;
					activeSelf.LastActivationTime    = -1;
					activeSelf.AddCombo(currentCommand.CommandTarget);
					
					isNewIncomingCommand = true;
				}

				// Checks for any active hero mode, it's important for the incoming step...
				var isHeroModeActive = false;
				if (activationCommandFromEntity.TryGet(activeSelf.Active, out var activeAbilityActivation)
				    && activeAbilityActivation.Type == EActivationType.HeroMode)
				{
					var abilityState = controllerFromEntity[activeSelf.Active];

					isHeroModeActive = true;
					// set inactivate if we can't chain it with the next command...
					if (activeAbilityActivation.Chaining != currentCommand.CommandTarget)
					{
						if (!activeAbilityActivation.HeroModeAllowedCommands.Contains(currentCommand.CommandTarget))
							isHeroModeActive = false;
						else if (isNewIncomingCommand)
							abilityState.ImperfectCountWhileActive++;
					}
					// set inactive if we had too much imperfect combo...
					else if (currentCommand.Power != 100 && activeAbilityActivation.HeroModeImperfectLimitBeforeDeactivation > 0
					                                     && isNewIncomingCommand)
					{
						abilityState.ImperfectCountWhileActive++;
					}
					else if (!gameCombo.IsFever)
						isHeroModeActive = false;

					if (abilityState.ImperfectCountWhileActive >= activeAbilityActivation.HeroModeImperfectLimitBeforeDeactivation)
						isHeroModeActive = false;

					controllerFromEntity[activeSelf.Active] = abilityState;
				}

				activeSelf.Incoming = isHeroModeActive ? activeSelf.Active : default;

				// Select correct abilities, and update state of owned abilities...
				var length       = container.Length;
				var priorityType = (int) (isHeroModeActive ? EActivationType.HeroMode : EActivationType.Normal);
				var priority     = -1;
				{
					var previousCommand = Entity.Null;
					var offset          = 1;
					if (currentCommand.ActiveAtTime > engineProcess.Milliseconds)
						offset++;

					var cmdIdx = activeSelf.CurrentCombo.Length - 1 - offset;
					if (cmdIdx >= 0 && activeSelf.CurrentCombo.Length >= cmdIdx + 1)
						previousCommand = activeSelf.CurrentCombo[cmdIdx];

					for (var i = 0; i != length; i++)
					{
						var actionEntity = container[i].Target;
						if (!controllerFromEntity.Exists(actionEntity))
							continue;

						var controller = controllerFromEntity[actionEntity];
						controller.Phase = EAbilityPhase.None;

						controllerFromEntity[actionEntity] = controller;

						abilityEngineFromEntity[actionEntity] = new AbilityEngineSet
						{
							Engine          = engineEntity,
							Process         = engineProcess,
							Settings        = engineSettings,
							CurrentCommand  = currentCommand,
							ComboState      = gameCombo,
							CommandState    = gameCommand,
							Command         = currentCommand.CommandTarget,
							PreviousCommand = previousCommand,
							Combo           = gameCombo,
							PreviousCombo   = default
						};

						var activation = activationCommandFromEntity[actionEntity];
						if (activation.Type == EActivationType.HeroMode && (!gameCombo.IsFever || !currentCommand.IsPerfect))
						{
							continue;
						}

						var commandPriority     = activation.Combos.Length;
						var commandPriorityType = (int) activation.Type;

						if (activation.Chaining == currentCommand.CommandTarget
						    && IsComboIdentical(activation.Combos, activeSelf.CurrentCombo)
						    && (activeSelf.Incoming == default || activeSelf.Incoming != default && ((priority < commandPriority && priorityType == commandPriorityType) || priorityType < commandPriorityType))
						    && (activation.Selection == gameCommand.Selection
						        || activeSelf.Incoming == default && activation.Selection == AbilitySelection.Horizontal))
						{
							if (activation.Selection == gameCommand.Selection)
							{
								priority = commandPriority;
							}

							priorityType        = (int) activation.Type;
							activeSelf.Incoming = actionEntity;
						}
					}
				}

				// If we are not chaining anymore or if the chain is finished, terminate our current command.
				if ((!gameCommand.HasActivity(engineProcess.Milliseconds, engineSettings.BeatInterval) && gameCommand.ChainEndTime < engineProcess.Milliseconds || gameCombo.Chain <= 0)
				    && activeSelf.Active != default)
				{
					if (controllerFromEntity.Exists(activeSelf.Active))
					{
						var controller = controllerFromEntity[activeSelf.Active];
						controller.Phase = EAbilityPhase.None;
						controller.Combo = 0;

						controllerFromEntity[activeSelf.Active] = controller;
					}

					activeSelf.Active = default;
				}

				if (activeSelf.Active != activeSelf.Incoming)
				{
					if (currentCommand.ActiveAtTime <= engineProcess.Milliseconds)
					{
						if (controllerFromEntity.TryGet(activeSelf.Active, out var previousState))
						{
							previousState.Combo                     = 0;
							controllerFromEntity[activeSelf.Active] = previousState;
						}

						activeSelf.Active = activeSelf.Incoming;
						if (activeSelf.Active != default)
						{
							var state = controllerFromEntity[activeSelf.Active];
							state.ImperfectCountWhileActive = 0;

							controllerFromEntity[activeSelf.Active] = state;
						}
					}
				}

				// We update incoming state before active state (in case if it's the same ability...)
				if (activeSelf.Incoming != default)
				{
					var incomingController = controllerFromEntity[activeSelf.Incoming];
					incomingController.Phase |= EAbilityPhase.WillBeActive;
					if (isNewIncomingCommand)
					{
						incomingController.UpdateVersion++;
						incomingController.Combo++;
						if (!isHeroModeActive)
							incomingController.ImperfectCountWhileActive = 0;
					}

					controllerFromEntity[activeSelf.Incoming] = incomingController;
				}

				if (activeSelf.Active != default)
				{
					var activeController = controllerFromEntity[activeSelf.Active];
					activeAbilityActivation = activationCommandFromEntity[activeSelf.Active];
					
					if (gameCommand.StartTime <= engineProcess.Milliseconds)
						activeController.Phase = EAbilityPhase.None;

					if (gameCommand.StartTime <= engineProcess.Milliseconds && activeSelf.LastActivationTime == -1)
					{
						activeSelf.LastActivationTime = engineProcess.Milliseconds;
						if (activeController.ActivationVersion == 0)
							activeController.ActivationVersion++;

						activeController.ActivationVersion++;

						if (activeAbilityActivation.Type == EActivationType.HeroMode && isServer)
						{
							gameCombo.JinnEnergy                   += 15;
							gameComboStateFromEntity[engineEntity] =  gameCombo;
						}
					}

					if (activeAbilityActivation.Type == EActivationType.HeroMode
					    && activeController.Combo <= 1 // only do it if it's the first combo...
					    && activeSelf.Active == activeSelf.Incoming // only if the next command is the same as the current one...
					    && gameCommand.StartTime + engineSettings.BeatInterval > engineProcess.Milliseconds)
					{
						// delay the command for the first frame
						activeController.Phase |= EAbilityPhase.HeroActivation;
					}

					if ((activeController.Phase & EAbilityPhase.HeroActivation) == 0)
						activeController.Phase |= gameCommand.IsGamePlayActive(engineProcess.Milliseconds) ? EAbilityPhase.Active : EAbilityPhase.Chaining;

					controllerFromEntity[activeSelf.Active] = activeController;
				}

				if (rhythmHeroStateFromEntity.Exists(engineEntity))
				{
					if (isHeroModeActive || (activeSelf.Incoming != default
					                         && activationCommandFromEntity[activeSelf.Incoming].Type == EActivationType.HeroMode))
					{
						/*if (!isServer)
							Debug.Log(controllerFromEntity[activeSelf.Incoming].Combo + ", " + isNewIncomingCommand);*/
						rhythmHeroStateFromEntity[engineEntity] = new RhythmHeroState
						{
							LastActiveTick = tick,
							ActivationTick = controllerFromEntity[activeSelf.Incoming].Combo <= 1 && isNewIncomingCommand ? tick : default
						};
					}
				}

			}).Run();
		}
	}
}