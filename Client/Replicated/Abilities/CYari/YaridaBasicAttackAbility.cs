using System;
using System.Numerics;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;

namespace PataNext.CoreAbilities.Mixed.CYari
{
    public struct YaridaBasicAttackAbility : IThrowSpearAbility
    {
        public TimeSpan AttackStart       { get; set; }
        public bool     DidAttack         { get; set; }
        public TimeSpan Cooldown          { get; set; }
        public TimeSpan DelayBeforeAttack { get; set; }
        public TimeSpan PauseAfterAttack  { get; set; }
        public Vector2  ThrowVelocity     { get; set; }
        public Vector2  Gravity           { get; set; }

        public class Register : RegisterGameHostComponentData<YaridaBasicAttackAbility>
        {
        }
    }
}