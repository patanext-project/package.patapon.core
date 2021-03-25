using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.CTate;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Units.CTate
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(DefaultMarchAbilityAnimation))]
	public class TaterazayCounterAnimationIdle : TriggerAnimationOnAbilityActivation<TaterazayCounterAbility>
	{
		protected override string AnimationClip => "Idle";
	}

	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(DefaultMarchAbilityAnimation))]
	public class TaterazayCounterAnimationTrigger : TriggerAnimationAbilityOnAttack<TaterazayCounterAbility.State>
	{

	}
}