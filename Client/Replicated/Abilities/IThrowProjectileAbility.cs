using UnityEngine;

namespace PataNext.CoreAbilities.Mixed
{
	public interface IThrowProjectileAbility : ISimpleAttackAbility
	{
		public Vector2 ThrowVelocity { get; }
		public Vector2 Gravity       { get; }
	}

	public interface IThrowProjectileAbilitySettings : SimpleAttackAbility.ISettings
	{
		public Vector2 ThrowVelocity { get; }
		public Vector2 Gravity       { get; }
	}
}