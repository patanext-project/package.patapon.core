using System;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;

namespace PataNext.CoreAbilities.Mixed.CTate
{
	public struct TaterazayCounterAbility : SimpleAttackAbility.ISettings
	{
		public float SendBackDamageFactorOnTrigger;
		public float SendBackDamageFactorAfterTrigger;

		public struct State : SimpleAttackAbility.IState
		{
			public int PreviousActivation;
		
			public double DamageStock;
			
			public TimeSpan AttackStart { get; set; }
			public bool     DidAttack   { get; set; }
			public TimeSpan Cooldown    { get; set; }
			
			public class Register : RegisterGameHostComponentData<State>
			{}
		}

		public TimeSpan DelayBeforeAttack { get; set; }
		public TimeSpan PauseAfterAttack  { get; set; }
		
		public class Register : RegisterGameHostComponentData<TaterazayCounterAbility>
		{}
	}
}