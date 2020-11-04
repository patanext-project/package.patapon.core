using PataNext.Client.Graphics.Animation.Base;
using PataNext.CoreAbilities.Mixed.CTate;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Units.CTate
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(DefaultMarchAbilityAnimation))]
	public class TaterazayBasicDefendFrontalAnimation : TriggerAnimationOnAbilityActivation<TaterazayBasicDefendFrontalAbility>
	{
	}
}