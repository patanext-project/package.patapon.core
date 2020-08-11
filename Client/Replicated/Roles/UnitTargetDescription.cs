using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<UnitTargetDescription>))]

namespace PataNext.Module.Simulation.Components.Roles
{
	public struct UnitTargetDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<UnitTargetDescription>.Register
		{
			public override ICustomComponentDeserializer BurstKnowDeserializer()
			{
				return new CustomSingleDeserializer<Relative<UnitTargetDescription>, Relative<UnitTargetDescription>.ValueDeserializer>();
			}
		}

		public class Register : RegisterGameHostComponentData<UnitTargetDescription>
		{
		}
	}
}