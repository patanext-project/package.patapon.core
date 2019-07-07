using System;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Entities;
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

		public Entity Command;
		public bool   IsActive;
		public int    ActiveId;
		public bool   IsStillChaining;

		public void CalculateWithValidCommand(GameCommandState commandState, GameComboState combo, RhythmEngineProcess process)
		{
			Calculate(new RhythmCurrentCommand {CommandTarget = Command}, commandState, combo, process);
		}

		public void Calculate(RhythmCurrentCommand currCommand, GameCommandState commandState, GameComboState combo, RhythmEngineProcess process)
		{
			if (currCommand.CommandTarget != Command)
			{
				IsActive        = false;
				IsStillChaining = false;
				return;
			}

			if (commandState.IsGamePlayActive(process.TimeTick))
			{
				IsActive = currCommand.CommandTarget == Command;
			}
			else
			{
				IsActive = false;
			}

			if (IsActive && PreviousActiveStartTime != commandState.StartTime)
			{
				PreviousActiveStartTime = commandState.StartTime;
				ActiveId++;
			}

			IsStillChaining = commandState.StartTime <= process.TimeTick && combo.Chain > 0;
		}
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class UpdateRhythmAbilityState : ComponentSystem
	{
		private void ForEach(ref Owner owner, ref RhythmAbilityState abilityState)
		{
			if (owner.Target == default || !EntityManager.Exists(owner.Target))
				return; // ????

			var engine = EntityManager.GetComponentData<Relative<RhythmEngineDescription>>(owner.Target).Target;
			if (!EntityManager.Exists(engine))
				return;

			var engineProcess  = EntityManager.GetComponentData<RhythmEngineProcess>(engine);
			var currentCommand = EntityManager.GetComponentData<RhythmCurrentCommand>(engine);
			var commandState   = EntityManager.GetComponentData<GameCommandState>(engine);
			var comboState     = EntityManager.GetComponentData<GameComboState>(engine);

			abilityState.Calculate(currentCommand, commandState, comboState, engineProcess);
		}

		private EntityQueryBuilder.F_DD<Owner, RhythmAbilityState> m_ForEachDelegate;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ForEachDelegate = ForEach;
		}

		protected override void OnUpdate()
		{
			Entities.ForEach(m_ForEachDelegate);
		}
	}
}