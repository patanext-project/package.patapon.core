using GameHost.Native;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Network
{
	public struct MasterServerControlledUnitData : IComponentData
	{
		public CharBuffer64 UnitGuid;

		public class Register : RegisterGameHostComponentData<MasterServerControlledUnitData>
		{
		}
	}
}