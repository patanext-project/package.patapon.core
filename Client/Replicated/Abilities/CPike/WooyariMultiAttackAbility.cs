using System;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;

namespace PataNext.CoreAbilities.Mixed.CPike
{
	public struct WooyariMultiAttackAbility : ISimpleAttackAbility
	{
		public int         AttackCount;
		public int         PreviousActivation;
		
		public EAttackType Current;
		public EAttackType Next;

		public ECombo Combo;

		public int MaxAttacksPerCommand { get; set; }

		public TimeSpan AttackStart       { get; set; }
		public bool     DidAttack         { get; set; }
		public TimeSpan Cooldown          { get; set; }
		public TimeSpan DelayBeforeAttack { get; set; }
		public TimeSpan PauseAfterAttack  { get; set; }

		public enum EAttackType
		{
			None     = 0,
			Uppercut = 1,
			Stab     = 2,
			Swing    = 3,
		}

		public enum ECombo
		{
			None,
			
			Stab,
			StabStab,
			StabStabStab,
			// stab then uppercut
			Slash,
			
			Swing,
			// swing multiple time
			Spin,
			// swing then stab
			Dash,
			
			// swing - uppercut
			PingPongUp,
			PingPongDown,

			Uppercut,
			UppercutThenStab,
		}

		public class Register : RegisterGameHostComponentData<WooyariMultiAttackAbility>
		{
		}
	}
}