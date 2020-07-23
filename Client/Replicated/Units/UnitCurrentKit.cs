using GameHost.Simulation.Utility.Resource;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Units
{
	public readonly struct UnitCurrentKit : IComponentData
	{
		public readonly GameResource<IUnitKitResource> Resource;

		public UnitCurrentKit(GameResource<IUnitKitResource> id)
		{
			Resource = id;
		}
	}
}