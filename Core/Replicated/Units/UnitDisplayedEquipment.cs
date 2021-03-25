using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Units
{
	public struct UnitDisplayedEquipment : IBufferElementData
	{
		public GameResource<UnitAttachmentResource> Attachment;
		public GameResource<EquipmentResource>      Resource;

		public class Register : RegisterGameHostComponentBuffer<UnitDisplayedEquipment>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultBufferDeserializer<UnitDisplayedEquipment>();
		}
	}
}