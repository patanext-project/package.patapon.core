using System;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;

namespace PataNext.CoreAbilities.Mixed.CTate
{
	public struct TaterazayBasicAttackAbility : ISimpleAttackAbility
	{
		public TimeSpan AttackStart       { get; set; }
		public bool     DidAttack         { get; set; }
		public TimeSpan Cooldown          { get; set; }
		public TimeSpan DelayBeforeAttack { get; set; }
		public TimeSpan PauseAfterAttack  { get; set; }
		
		public class Register : RegisterGameHostComponentData<TaterazayBasicAttackAbility>
		{}
	}
}