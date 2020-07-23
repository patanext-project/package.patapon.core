using GameHost.Native;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Units
{
	public struct UnitDisplayedEquipment : IBufferElementData
	{
		public CharBuffer32 Attachment;
		public CharBuffer64 ResourceId;

		public class Register : RegisterGameHostComponentBuffer<UnitDisplayedEquipment>
		{
		}
	}
}