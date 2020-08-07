using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.GameBase.Physics.Components;
using Unity.Transforms;

namespace PataNext.Client.Core.UnityTypes
{
	public class RegisterVelocity : RegisterGameHostComponentData<Velocity>
	{
		protected override string CustomComponentPath => "StormiumTeam.GameBase.Physics.Components::Velocity";
	}
}