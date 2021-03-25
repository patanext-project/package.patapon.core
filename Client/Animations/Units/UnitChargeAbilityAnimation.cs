using PataNext.Client.Graphics.Animation.Base;
using PataNext.CoreAbilities.Mixed.CTate;
using PataNext.CoreAbilities.Mixed.Defaults;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Units.CTate
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(DefaultMarchAbilityAnimation))]
	public class UnitChargeAbilityAnimation : TriggerAnimationOnAbilityActivation<DefaultChargeAbility>
	{
	}
}