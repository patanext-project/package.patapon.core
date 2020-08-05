using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Interfaces;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(Relative<RhythmEngineDescription>))]

namespace PataNext.Module.Simulation.Components.Roles
{
	public struct RhythmEngineDescription : IEntityDescription
	{
		public class RegisterRelative : Relative<RhythmEngineDescription>.Register
		{
		}

		public class Register : RegisterGameHostComponentData<RhythmEngineDescription>
		{
		}
	}
}