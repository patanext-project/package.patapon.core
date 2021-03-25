using System;
using GameHost.ShareSimuWorldFeature.Systems;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.CoreAbilities.Mixed;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.Mathematics;

namespace PataNext.Client.Graphics.Animation.Units.Base
{
	public abstract class TriggerAnimationAbilityOnAttack<TAbility> : TriggerAnimationAbilitySystem<TAbility, TimeSpan>
		where TAbility : struct, IComponentData, SimpleAttackAbility.IState
	{
		private readonly string attackTrigger = "OnActivate";

		protected override string[] GetTriggers()
		{
			return new[] {attackTrigger};
		}

		public override bool AllowOverride => false;

		public virtual float StopOffset => 0.2f;

		private TimeSystem timeSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			timeSystem = World.GetExistingSystem<TimeSystem>();
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation, Entity abilityEntity, AbilityState abilityState)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type == SystemType && currAnim.StopAt < animation.RootTime)
			{
				// allow transitions and overrides now...
				animation.SetTargetAnimation(new TargetAnimation(currAnim.Type, transitionStart: currAnim.StopAt, transitionEnd: currAnim.StopAt + StopOffset));
				if (animation.RootTime > currAnim.StopAt + StopOffset)
					Stop();
			}
			
			var ability = GetComponent<TAbility>(abilityEntity);
			if (ability.AttackStart <= TimeSpan.Zero)
			{
				return;
			}

			ref var systemData = ref animation.GetSystemData<SystemData>(SystemType);
			if (!systemData.LoadedClips.TryGetValue(attackTrigger, out var clip) || clip == null)
				return;

			if (ability.AttackStart != systemData.Supplements)
			{
				Trigger(attackTrigger);
				animation.SetTargetAnimation(new TargetAnimation(SystemType, false, false, stopAt: animation.RootTime + (clip.length - StopOffset)));
				
				systemData.Supplements = ability.AttackStart;

				var gameTime             = GetSingleton<InterFrame>().End;
				var aheadStartDifference = (float) math.max(gameTime.Elapsed - ability.AttackStart.Seconds, 0);
				//systemData.StartTime -= math.clamp(aheadStartDifference, -0.2f, 0.2f);
			}
		}
	}
}