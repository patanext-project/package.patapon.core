using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Units
{
	public readonly struct UnitCurrentKit : IComponentData
	{
		public readonly GameResource<UnitKitResource> Resource;

		public UnitCurrentKit(GameResource<UnitKitResource> id)
		{
			Resource = id;
		}
		
		public class Register : RegisterGameHostComponentData<UnitCurrentKit>
		{}
	}
}