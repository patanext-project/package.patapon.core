﻿using GameHost.Simulation.Utility.Resource;
 using GameHost.Simulation.Utility.Resource.Interfaces;
 using PataNext.Module.Simulation.Resources;
 using Unity.Entities;

 [assembly: RegisterGenericComponentType(typeof(GameResource<UnitAttachmentResource>))]
 
namespace PataNext.Module.Simulation.Resources
{
	/// <summary>
	/// Represent the attachment of equipment and cosmetic on an entity
	/// </summary>
	public struct UnitAttachmentResource : IGameResourceDescription
	{
	}
}