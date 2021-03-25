using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.CMega;
using PataNext.CoreAbilities.Mixed.CTate;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Units.CTate
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(DefaultMarchAbilityAnimation))]
	public class MegaponBasicSonicAttackAnimation : TriggerAnimationAbilityOnAttack<MegaponBasicSonicAttackAbility.State>
	{
		protected override string AnimationPrefix => $"MegaponBasicSonicAttackAbility/Animations/";
	}
}