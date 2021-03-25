using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.Special
{
	public struct UnitBodyCollider : IComponentData
	{
		public float Width;
		public float Height;
		public float Scale;

		public UnitBodyCollider(float width, float height)
		{
			Width  = width;
			Height = height;
			Scale  = 1;
		}

		public class Register : RegisterGameHostComponentData<UnitBodyCollider>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<UnitBodyCollider>();
		}
	}
}