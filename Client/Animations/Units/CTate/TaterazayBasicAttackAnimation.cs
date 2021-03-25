using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.CTate;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Units.CTate
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(DefaultMarchAbilityAnimation))]
	public class TaterazayBasicAttackAnimation : TriggerAnimationAbilityOnAttack<TaterazayBasicAttackAbility.State>
	{
		protected override string AnimationPrefix => $"TaterazayBasicAttackAbility/Animations/";
	}
}