using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units;
using PataNext.CoreAbilities.Mixed.CGuard;
using Unity.Entities;

namespace PataNext.Client.Animations.Units.CGuard
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(DefaultMarchAbilityAnimation))]
	public class GuardiraBasicDefendAnimation : TriggerAnimationOnAbilityActivation<GuardiraBasicDefendAbility>
	{
		
	}
}