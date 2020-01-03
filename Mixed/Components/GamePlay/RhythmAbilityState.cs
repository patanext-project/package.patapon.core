using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine.Flow;
using Unity.Entities;

namespace Patapon.Mixed.GamePlay
{
	/// <summary>
	/// Rhythm based ability.
	/// </summary>
	public struct RhythmAbilityState : IComponentData
	{
		internal int PreviousActiveStartTime;

		public GameComboState Combo;

		public Entity Engine;
		public Entity Command;
		public bool   IsActive;
		public int    ActiveId;
		public bool   IsStillChaining;
		public bool   WillBeActive;
		public int    StartTime;

		public void CalculateWithValidCommand(GameCommandState commandState, GameComboState combo, FlowEngineProcess process)
		{
			Calculate(new RhythmCurrentCommand {CommandTarget = Command}, commandState, combo, process);
		}

		public void Calculate(RhythmCurrentCommand currCommand, GameCommandState commandState, GameComboState combo, FlowEngineProcess process)
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
}