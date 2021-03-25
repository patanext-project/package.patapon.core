using System;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using UnityEngine;

namespace PataNext.CoreAbilities.Mixed.CMega
{
	public struct MegaponBasicSonicAttackAbility : IThrowProjectileAbilitySettings
	{
		public TimeSpan DelayBeforeAttack { get; set; }
		public TimeSpan PauseAfterAttack  { get; set; }

		public struct State : SimpleAttackAbility.IState
		{
			public TimeSpan AttackStart { get; set; }
			public bool     DidAttack   { get; set; }
			public TimeSpan Cooldown    { get; set; }

			public class Register : RegisterGameHostComponentData<State>
			{
				protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<State>();
			}
		}

		public Vector2 ThrowVelocity { get; set; }
		public Vector2 Gravity       { get; set; }
		
		public class Register : RegisterGameHostComponentData<State>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<MegaponBasicSonicAttackAbility>();
		}
	}
}