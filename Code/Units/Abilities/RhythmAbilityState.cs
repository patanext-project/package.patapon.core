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
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class UpdateRhythmAbilityState : ComponentSystem
	{
		private void ForEach(ref Owner owner, ref RhythmAbilityState abilityState)
		{
			var engine         = EntityManager.GetComponentData<Relative<RhythmEngineDescription>>(owner.Target).Target;
			var engineProcess  = EntityManager.GetComponentData<RhythmEngineProcess>(engine);
			var currentCommand = EntityManager.GetComponentData<RhythmCurrentCommand>(engine);
			var commandState   = EntityManager.GetComponentData<GameCommandState>(engine);
			var comboState = EntityManager.GetComponentData<GameComboState>(engine);

			if (currentCommand.CommandTarget != abilityState.Command)
			{
				abilityState.IsActive = false;
				abilityState.IsStillChaining = false;
				return;
			}

			if (commandState.IsGamePlayActive(engineProcess.TimeTick))
			{
				abilityState.IsActive = EntityManager.GetComponentData<RhythmCurrentCommand>(engine).CommandTarget == abilityState.Command;
			}
			else
			{
				abilityState.IsActive = false;
			}

			if (abilityState.IsActive && abilityState.PreviousActiveStartTime != commandState.StartTime)
			{
				abilityState.PreviousActiveStartTime = commandState.StartTime;
				abilityState.ActiveId++;
			}

			abilityState.IsStillChaining = commandState.StartTime <= engineProcess.TimeTick && comboState.Chain > 0;
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