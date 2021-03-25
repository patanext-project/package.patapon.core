using PataNext.Client.Graphics.Animation.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Units.Base
{
	public class TriggerAnimationOnHeroModeActivation<TAbility> : TriggerAnimationOnAbilityActivation<TAbility>
		where TAbility : struct, IComponentData
	{
		public override    bool          AllowOverride        => false;
		public override    EAbilityPhase KeepAnimationAtPhase => EAbilityPhase.HeroActivation;
		protected override string        AnimationClip        => "OnHeroModeActivate";
	}
}