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
		public Entity Command;
		public bool   IsActive;
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class UpdateRhythmAbilityState : ComponentSystem
	{
		private void ForEach(ref Owner owner, ref RhythmAbilityState abilityState)
		{
			var engine        = EntityManager.GetComponentData<Relative<RhythmEngineDescription>>(owner.Target).Target;
			var engineProcess = EntityManager.GetComponentData<RhythmEngineProcess>(engine);
			var commandState  = EntityManager.GetComponentData<GameCommandState>(engine);

			if (commandState.IsGamePlayActive(engineProcess.TimeTick))
			{
				abilityState.IsActive = EntityManager.GetComponentData<RhythmCurrentCommand>(engine).CommandTarget == abilityState.Command;
			}
			else
			{
				abilityState.IsActive = false;
			}
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