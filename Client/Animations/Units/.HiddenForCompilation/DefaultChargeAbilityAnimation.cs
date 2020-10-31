using System.Collections.Generic;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Client.Graphics.Animation.Units.CTate;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Simulation.Mixed.Abilities.CTate;
using PataNext.Simulation.Mixed.Abilities.Defaults;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	public class TaterazayStayDefendAnimation : SingleAnimationSystemBase
	{
		private readonly AddressBuilderClient m_AddrPath = AddressBuilder.Client()
		                                                                 .Folder("Models")
		                                                                 .Folder("UberHero")
		                                                                 .Folder("Animations")
		                                                                 .Folder("Shared");

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(AbilityState), typeof(DefaultChargeAbility), typeof(Owner)}
			});
		}

		public override string DefaultResourceClip => m_AddrPath.GetFile("Charge.anim");
		public override string DefaultKeyClip      => "defaultCharge/anim.clip";
	}
}