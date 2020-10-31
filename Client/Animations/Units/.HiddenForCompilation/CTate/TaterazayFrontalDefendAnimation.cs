using PataNext.Client.Core.Addressables;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Simulation.Mixed.Abilities.CTate;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Units.CTate
{
	public class TaterazayFrontalDefendAnimation : SingleAnimationSystemBase
	{
		private readonly AddressBuilderClient m_AddrPath = AddressBuilder.Client()
		                                                                 .Folder("Models")
		                                                                 .Folder("UberHero")
		                                                                 .Folder("Animations")
		                                                                 .Folder("Taterazay");
		
		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(AbilityState), typeof(TaterazayBasicDefendFrontalAbility), typeof(Owner)},
			});
		}

		public override string DefaultResourceClip => m_AddrPath.GetFile("TaterazayBasicDefend.anim");
		public override string DefaultKeyClip      => "tate/basicDefend/walking.clip";
	}
}