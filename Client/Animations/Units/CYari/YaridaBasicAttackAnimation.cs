using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.CYari;

namespace PataNext.Client.Graphics.Animation.Units.CYari
{
	public class YaridaBasicAttackAnimation : TriggerAnimationAbilityOnAttack<YaridaBasicAttackAbility.State>
	{
		protected override string AnimationPrefix => $"YaridaBasicAttackAbility/Animations/";
	}
}