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
		public int             HeroModeMaxCombo;
		public int             HeroModeImperfectLimitBeforeDeactivation;

		public AbilitySelection Selection;

		/// <summary>
		/// The command used for chaining.
		/// </summary>
		public Entity Chaining;

		/// <summary>
		/// Combo command list, excluding the chaining command.
		/// </summary>
		public FixedBuffer32<Entity> Combos; //< 32 bytes should suffice, it would be 4 combo commands...

		/// <summary>
		/// Allowed commands for chaining in hero mode.
		/// </summary>
		public FixedBuffer64<Entity> HeroModeAllowedCommands; //< 64 bytes should suffice, it would be up to 8 commands...

		public class Register : RegisterGameHostComponentData<AbilityActivation>
		{
		}
	}
}