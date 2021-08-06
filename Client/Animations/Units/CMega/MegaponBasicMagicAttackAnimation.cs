using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.CMega;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Units.CTate
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(DefaultMarchAbilityAnimation))]
	public class MegaponBasicMagicAttackAnimation : TriggerAnimationAbilityOnAttack<MegaponBasicMagicAttackAbility.State>
	{
		protected override string AnimationPrefix => $"MegaponBasicMagicAttackAbility/Animations/";
	}
}