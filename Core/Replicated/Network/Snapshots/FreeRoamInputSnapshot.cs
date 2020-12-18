using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.InterTick;
using Unity.Entities;

namespace PataNext.Module.Simulation.Network.Snapshots
{
	public struct FreeRoamInputSnapshot : IBufferElementData
	{
		public uint Tick { get; set; }

		public uint                  HorizontalMovement;
		public InterFramePressAction Up, Down;

		public class Register : RegisterGameHostComponentBuffer<FreeRoamInputSnapshot>
		{
		}
	}
}