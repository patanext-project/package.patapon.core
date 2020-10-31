using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.Resource;
using PataNext.Module.Simulation.Resources;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Units
{
	public readonly struct UnitArchetype : IComponentData
	{
		public readonly GameResource<UnitArchetypeResource> Resource;

		public UnitArchetype(GameResource<UnitArchetypeResource> id)
		{
			Resource = id;
		}

		public class Register : RegisterGameHostComponentData<UnitArchetype>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<UnitArchetype>();
		}
	}
}