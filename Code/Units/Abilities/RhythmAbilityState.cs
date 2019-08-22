using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Default
{
	/// <summary>
	/// Rhythm based ability.
	/// </summary>
	public struct RhythmAbilityState : IComponentData
	{
		internal int PreviousActiveStartTime;

		public GameComboState Combo;
		
		public Entity Command;
		public bool   IsActive;
		public int    ActiveId;
		public bool   IsStillChaining;
		public bool   WillBeActive;
		public int    StartTime;

		public void CalculateWithValidCommand(GameCommandState commandState, GameComboState combo, RhythmEngineProcess process)
		{
			Calculate(new RhythmCurrentCommand {CommandTarget = Command}, commandState, combo, process);
		}

		public void Calculate(RhythmCurrentCommand currCommand, GameCommandState commandState, GameComboState combo, RhythmEngineProcess process)
		{
			if (ActiveId == 0)
				ActiveId++;
			
			if (currCommand.CommandTarget != Command)
			{
				IsActive        = IsActive && commandState.StartTime > process.Milliseconds && currCommand.Previous == Command;
				IsStillChaining = IsStillChaining && commandState.StartTime > process.Milliseconds && currCommand.Previous == Command;
				StartTime       = -1;
				WillBeActive    = false;
				return;
			}

			IsActive = commandState.IsGamePlayActive(process.Milliseconds);

			if (IsActive && PreviousActiveStartTime != commandState.StartTime)
			{
				PreviousActiveStartTime = commandState.StartTime;
				ActiveId++;
			}

			Combo = combo;

			StartTime = commandState.StartTime;

			IsStillChaining = commandState.StartTime <= process.Milliseconds + (IsStillChaining ? 500 : 0) && combo.Chain > 0;
			WillBeActive    = commandState.StartTime > process.Milliseconds && process.Milliseconds <= commandState.EndTime && !IsActive;
		}
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class UpdateRhythmAbilityState : JobComponentSystem
	{
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<Owner, RhythmAbilityState>
		{
			[ReadOnly]
			public ComponentDataFromEntity<Relative<RhythmEngineDescription>> RelativeRhythmEngineFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<RhythmEngineDescription> RhythmEngineDescriptionFromEntity;

			[ReadOnly] public ComponentDataFromEntity<RhythmEngineProcess>  RhythmEngineProcessFromEntity;
			[ReadOnly] public ComponentDataFromEntity<RhythmCurrentCommand> RhythmCurrentCommandFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GameCommandState>     GameCommandStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GameComboState>       GameComboStateFromEntity;

			[BurstDiscard]
			private static void NonBurst_ErrorNoOwnerOrNoRelative(Entity owner, Entity entity)
			{
				if (owner == default)
					Debug.LogError($"Default owner found on " + entity);
				/*else
					Debug.LogError($"No RhythmEngine found on owner({owner}) of ability({entity})");*/
			}

			[BurstDiscard]
			private static void NonBurst_ErrorNoRhythmEngine(Entity target, Entity entity)
			{
				Debug.LogError($"No RhythmEngine found on target({target}) of ability({entity})");
			}

			public void Execute(Entity entity, int index, ref Owner owner, ref RhythmAbilityState abilityState)
			{
				if (owner.Target == default || !RelativeRhythmEngineFromEntity.Exists(owner.Target))
				{
					NonBurst_ErrorNoOwnerOrNoRelative(owner.Target, entity);
					return;
				}

				var engine = RelativeRhythmEngineFromEntity[owner.Target].Target;
				if (!RhythmEngineDescriptionFromEntity.Exists(engine))
				{
					NonBurst_ErrorNoRhythmEngine(engine, entity);
					return;
				}

				abilityState.Calculate(RhythmCurrentCommandFromEntity[engine],
					GameCommandStateFromEntity[engine],
					GameComboStateFromEntity[engine],
					RhythmEngineProcessFromEntity[engine]);
			}
		}


		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				RelativeRhythmEngineFromEntity    = GetComponentDataFromEntity<Relative<RhythmEngineDescription>>(true),
				RhythmEngineDescriptionFromEntity = GetComponentDataFromEntity<RhythmEngineDescription>(true),
				RhythmEngineProcessFromEntity     = GetComponentDataFromEntity<RhythmEngineProcess>(true),
				RhythmCurrentCommandFromEntity    = GetComponentDataFromEntity<RhythmCurrentCommand>(true),
				GameCommandStateFromEntity        = GetComponentDataFromEntity<GameCommandState>(true),
				GameComboStateFromEntity          = GetComponentDataFromEntity<GameComboState>(true),
			}.Schedule(this, inputDeps);
		}
	}
}