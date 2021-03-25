using System;
using Unity.Entities;

namespace PataNext.CoreAbilities.Mixed
{
    public interface ISimpleAttackAbility : IComponentData
    {
        // Can only attack if Cooldown is passed, and if there are no delay before next attack
        public TimeSpan AttackStart { get; set; }

        // prev: HasThrown, HasSlashed, ...
        public bool DidAttack { get; set; }

        /// <summary>
        /// Cooldown before waiting for the next attack
        /// </summary>
        public TimeSpan Cooldown { get; set; }

        /// <summary>
        /// Delay before the attack (does not include <see cref="Cooldown"/>)
        /// </summary>
        public TimeSpan DelayBeforeAttack { get; }

        /// <summary>
        /// Delay after the attack (does not include <see cref="Cooldown"/>)
        /// </summary>
        public TimeSpan PauseAfterAttack { get; }
    }

    public static class SimpleAttackAbility
    {
        public interface IState : IComponentData
        {
            // Can only attack if Cooldown is passed, and if there are no delay before next attack
            public TimeSpan AttackStart { get; set; }

            // prev: HasThrown, HasSlashed, ...
            public bool DidAttack { get; set; }

            /// <summary>
            /// Cooldown before waiting for the next attack
            /// </summary>
            public TimeSpan Cooldown { get; set; }
        }

        public interface ISettings : IComponentData
        {
            /// <summary>
            /// Delay before the attack (does not include <see cref="Cooldown"/>)
            /// </summary>
            public TimeSpan DelayBeforeAttack { get; }

            /// <summary>
            /// Delay after the attack (does not include <see cref="Cooldown"/>)
            /// </summary>
            public TimeSpan PauseAfterAttack { get; }
        }
    }
}