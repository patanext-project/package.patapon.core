using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<ProjectileDescription>))]

namespace PataNext.Module.Simulation.Components.Roles
{
	public struct ProjectileDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<ProjectileDescription>.Register
		{
			public override ICustomComponentDeserializer BurstKnowDeserializer()
			{
				return new CustomSingleDeserializer<Relative<ProjectileDescription>, Relative<ProjectileDescription>.ValueDeserializer>();
			}
		}

		public class Register : RegisterGameHostComponentData<ProjectileDescription>
		{
		}
	}
}