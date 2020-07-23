using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Units
{
	public struct UnitDisplayedEquipment : IBufferElementData
	{
		public GameResource<IUnitAttachmentResource> Attachment;
		public GameResource<IEquipmentResource>      Resource;

		public class Register : RegisterGameHostComponentBuffer<UnitDisplayedEquipment>
		{
		}
	}
}