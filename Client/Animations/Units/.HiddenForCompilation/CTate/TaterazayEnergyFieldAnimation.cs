using Client.Graphics.Animation.Units;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Simulation.Mixed.Abilities.CTate;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Units.CTate
{
	public class TaterazayEnergyFieldActivationAnimation : HeroModeActivationAnimationSystemBase
	{
		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(TaterazayEnergyFieldAbility), typeof(AbilityState), typeof(Owner)}
			});
		}
		
		private readonly AddressBuilderClient m_AddrPath = AddressBuilder.Client()
		                                                                 .Folder("Models")
		                                                                 .Folder("UberHero")
		                                                                 .Folder("Animations")
		                                                                 .Folder("Taterazay")
		                                                                 .Folder("EnergyField");

		public override string DefaultResourceClip => m_AddrPath.GetFile("activation.anim");
	}

	public class TaterazayEnergyFieldAnimation : SingleAnimationSystemBase
	{
		private readonly AddressBuilderClient m_AddrPath = AddressBuilder.Client()
		                                                                 .Folder("Models")
		                                                                 .Folder("UberHero")
		                                                                 .Folder("Animations")
		                                                                 .Folder("Taterazay")
		                                                                 .Folder("EnergyField");

		public override bool          AllowOverride        => false;
		public override EAbilityPhase KeepAnimationAtPhase => EAbilityPhase.ActiveOrChaining;
		
		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(TaterazayEnergyFieldAbility), typeof(AbilityState), typeof(Owner)}
			});
		}

		public override string DefaultResourceClip => m_AddrPath.GetFile("loop_idle.anim");
		public override string DefaultKeyClip      => "tate/energyField/loop_idle.clip";
	}
}