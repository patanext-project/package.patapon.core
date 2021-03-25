using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.CYari;

namespace PataNext.Client.Graphics.Animation.Units.CYari
{
	public class YaridaLeapAttackAnimation : TriggerAnimationAbilityOnAttack<YaridaLeapAttackAbility.State>
	{
		protected override string AnimationPrefix => $"YaridaLeapAttackAbility/Animations/";
	}
}