using UnityEngine;

namespace PataNext.CoreAbilities.Mixed.CYari
{
    public interface IThrowSpearAbility : ISimpleAttackAbility
    {
        public Vector2 ThrowVelocity { get; }
        public Vector2 Gravity { get; }
    }
}