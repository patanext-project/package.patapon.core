using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<UnitDescription>))]

namespace PataNext.Module.Simulation.Components.Roles
{
	public struct UnitDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<UnitDescription>.Register
		{
		}

		public class Register : RegisterGameHostComponentData<UnitDescription>
		{
		}
	}
}