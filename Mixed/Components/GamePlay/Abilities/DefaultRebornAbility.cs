using Unity.Entities;

namespace Patapon.Mixed.GamePlay.Abilities
{
	// Active only when you're eliminated
	public struct DefaultRebornAbility : IComponentData
	{
		public bool WasFever;
		public int LastPressureBeat;
		
		public class Provider : BaseRhythmAbilityProvider<DefaultRebornAbility> {}
	}
}