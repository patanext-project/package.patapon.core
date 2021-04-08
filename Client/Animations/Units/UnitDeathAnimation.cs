using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.Defaults;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using StormiumTeam.GameBase.GamePlay.Health;
using Unity.Entities;

namespace PataNext.Client.Animations.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(DefaultMarchAbilityAnimation))]
	public class UnitDeathAnimation : TriggerAnimationSystem
	{
		public override string FolderName => "OnElimination";

		public override bool AllowOverride => false;

		protected override string[] GetTriggers()
		{
			return new[] {"Death"};
		}

		protected override bool IsValidTarget(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			return HasComponent<LivableHealth>(targetEntity);
		}

		protected override bool ShouldStopAnimation(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			return false == HasComponent<LivableIsDead>(targetEntity);
		}

		protected override void OnUpdateForTriggers(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			if (animation.CurrAnimation.Type != SystemType)
			{
				Trigger("Death");
			}
		}
	}
}