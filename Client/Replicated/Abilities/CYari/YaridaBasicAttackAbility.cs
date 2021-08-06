using System;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Vector2 = UnityEngine.Vector2;

namespace PataNext.CoreAbilities.Mixed.CYari
{
    public struct YaridaBasicAttackAbility : IThrowProjectileAbilitySettings
    {
        public TimeSpan DelayBeforeAttack { get; }
        public TimeSpan PauseAfterAttack  { get; }
        public Vector2  ThrowVelocity     { get; }
        public Vector2  Gravity           { get; }
        
        public class Register : RegisterGameHostComponentData<YaridaBasicAttackAbility>
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