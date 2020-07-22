using GameBase.Roles.Components;
using GameBase.Roles.Interfaces;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.Roles;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<UnitTargetDescription>))]

namespace PataNext.Module.Simulation.Components.Roles
{
	public struct UnitTargetDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<UnitTargetDescription>.Register
		{
		}

		public class Register : RegisterGameHostComponentSystemBase<UnitTargetDescription>
		{
		}
	}
}