using System.Runtime.CompilerServices;
using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GameModes.City
{
	// No need to replicate it for now
	/*public struct CityScenes : IBufferElementData
	{
		public Entity Entity;

		public CityScenes(Entity entity)
		{
			Entity = entity;
		}

		public class Register : RegisterGameHostComponentBuffer<CityScenes>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<CityScenes, >()
		}
	}*/

	public struct CityLocationTag : IComponentData
	{
		public class Register : RegisterGameHostComponentData<CityLocationTag> {}
	}

	public struct PlayerCurrentCityLocation : IComponentData
	{
		public Entity Entity;

		public PlayerCurrentCityLocation(Entity entity)
		{
			Entity = entity;
		}

		public class Register : RegisterGameHostComponentData<PlayerCurrentCityLocation>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<PlayerCurrentCityLocation, Serializer>();
		}

		public struct Serializer : IValueDeserializer<PlayerCurrentCityLocation>
		{
			public int Size => Unsafe.SizeOf<GhGameEntitySafe>();

			public void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghEntityToUEntity, ref PlayerCurrentCityLocation component, ref DataBufferReader reader)
			{
				ghEntityToUEntity.TryGetValue(reader.ReadValue<GhGameEntitySafe>(), out component.Entity);
			}
		}
	}
}