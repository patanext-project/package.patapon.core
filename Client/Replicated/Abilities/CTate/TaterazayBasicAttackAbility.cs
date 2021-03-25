using System;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;

namespace PataNext.CoreAbilities.Mixed.CTate
{
	public struct TaterazayBasicAttackAbility : SimpleAttackAbility.ISettings
	{
		public TimeSpan DelayBeforeAttack { get; set; }
		public TimeSpan PauseAfterAttack  { get; set; }

		public class Register : RegisterGameHostComponentData<TaterazayBasicAttackAbility>
		{
		}

		public struct State : SimpleAttackAbility.IState
		{
			public TimeSpan AttackStart { get; set; }
			public bool     DidAttack   { get; set; }
			public TimeSpan Cooldown    { get; set; }

			public class Register : RegisterGameHostComponentData<State>
			{
			}
		}
	}
}