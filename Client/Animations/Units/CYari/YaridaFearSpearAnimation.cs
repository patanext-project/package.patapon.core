using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.CYari;

namespace PataNext.Client.Graphics.Animation.Units.CYari
{
	public class YaridaFearSpearAnimationHeroModeActivation : TriggerAnimationOnHeroModeActivation<YaridaFearSpearAbility>
	{
		
	}
	
	public class YaridaFearSpearAnimation : TriggerAnimationAbilityOnAttack<YaridaFearSpearAbility.State>
	{
		protected override string AnimationPrefix => $"YaridaFearSpearAbility/Animations/";
	}
}