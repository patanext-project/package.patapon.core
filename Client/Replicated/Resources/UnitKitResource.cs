using GameHost.Simulation.Utility.Resource;
using GameHost.Simulation.Utility.Resource.Interfaces;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

[assembly: RegisterGenericComponentType(typeof(GameResource<UnitKitResource>))]

namespace PataNext.Module.Simulation.Resources
{
	/// <summary>
	/// An <see cref="UnitDescription"/> kit (taterazay, yarida, ...) resource description
	/// </summary>
	public struct UnitKitResource : IGameResourceDescription
	{
	}
}