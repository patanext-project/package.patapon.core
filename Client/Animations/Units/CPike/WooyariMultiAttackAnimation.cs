using System;
using System.Collections.Generic;
using System.Linq;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.CPike;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using Unity.Entities;
using UnityEngine;

namespace PataNext.Client.Graphics.Animation.Units.CPike
{
	public class WooyariMultiAttackAnimation : TriggerAnimationAbilitySystem<WooyariMultiAttackAbility, TimeSpan>
	{
		private Dictionary<WooyariMultiAttackAbility.ECombo, string> triggerMap;
		
		protected override string[] GetTriggers()
		{
			triggerMap = new Dictionary<WooyariMultiAttackAbility.ECombo, string>();
			foreach (var value in Enum.GetValues(typeof(WooyariMultiAttackAbility.ECombo)))
				triggerMap[(WooyariMultiAttackAbility.ECombo) value] = ((WooyariMultiAttackAbility.ECombo) value).ToString();
			
			return triggerMap.Values.ToArray();
		}

		public override bool AllowOverride => false;

		public virtual float StopOffset => 0.2f;

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

			var ability = GetComponent<WooyariMultiAttackAbility>(abilityEntity);
			if (ability.AttackStart <= TimeSpan.Zero)
			{
				return;
			}

			ref var systemData = ref animation.GetSystemData<SystemData>(SystemType);

			var toPlay = triggerMap[ability.Combo];
			if (!systemData.LoadedClips.TryGetValue(toPlay, out var clip))
				return;

			if (ability.AttackStart != systemData.Supplements)
			{
				Debug.Log("Play: " + toPlay);

				systemData.Supplements = ability.AttackStart;
				if (clip == null)
					return;

				Trigger(toPlay);
				animation.SetTargetAnimation(new TargetAnimation(SystemType, false, false, stopAt: animation.RootTime + (clip.length - StopOffset)));
			}
		}
	}
}