﻿using GameHost.Simulation.Utility.Resource;
 using GameHost.Simulation.Utility.Resource.Interfaces;
using PataNext.Module.Simulation.Components.Roles;
 using PataNext.Module.Simulation.Resources;
 using Unity.Entities;

 [assembly: RegisterGenericComponentType(typeof(GameResource<EquipmentResource>))]
 
namespace PataNext.Module.Simulation.Resources
{
	/// <summary>
	/// Represent an equipment resource (or cosmetic) that would be usable on <see cref="UnitDescription"/>
	/// </summary>
	public struct EquipmentResource : IGameResourceDescription
	{
	}
}