using GameBase.Roles.Components;
using GameBase.Roles.Interfaces;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.Roles;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<UnitDescription>))]

namespace PataNext.Module.Simulation.Components.Roles
{
	public struct UnitDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<UnitDescription>.Register
		{
		}

		public class Register : RegisterGameHostComponentSystemBase<UnitDescription>
		{
		}
	}
}