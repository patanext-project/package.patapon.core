using GameHost.Native;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.Abilities
{
	public enum EAbilityActivationType
	{
		Normal,
		HeroMode,
		Custom
	}

	public struct AbilityActivation : IComponentData
	{
		public EAbilityActivationType Type;
		public int                    HeroModeMaxCombo;
		public int                    HeroModeImperfectLimitBeforeDeactivation;

		public AbilitySelection Selection;

		public class Register : RegisterGameHostComponentData<AbilityActivation>
		{
		}
	}
}