using System.Runtime.InteropServices;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using PataNext.Client.Systems;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Units
{
	// it's like DisplayedEquipment, but with stats modification
	public struct UnitDefinedEquipments : IBufferElementData
	{
		public GameResource<UnitAttachmentResource> Attachment;
		public DentEntity                           Item;

		public class Register : RegisterGameHostComponentBuffer<UnitDefinedEquipments>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultBufferDeserializer<UnitDefinedEquipments>();
		}
	}
}