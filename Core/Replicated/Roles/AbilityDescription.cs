using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<AbilityDescription>))]

namespace PataNext.Module.Simulation.Components.Roles
{
	public struct AbilityDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<AbilityDescription>.Register
		{
			public override ICustomComponentDeserializer BurstKnowDeserializer()
			{
				return new CustomSingleDeserializer<Relative<AbilityDescription>, Relative<AbilityDescription>.ValueDeserializer>();
			}
		}

		public class Register : RegisterGameHostComponentData<AbilityDescription>
		{
		}
	}
}