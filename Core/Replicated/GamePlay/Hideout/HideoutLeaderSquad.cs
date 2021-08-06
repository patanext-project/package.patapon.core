using System.Runtime.CompilerServices;
using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.Hideout
{
	public struct HideoutLeaderSquad : IComponentData
	{
		public Entity Leader;

		public struct Deserializer : IValueDeserializer<HideoutLeaderSquad>
		{
			public int Size => Unsafe.SizeOf<GhGameEntitySafe>();

			public void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghEntityToUEntity, ref HideoutLeaderSquad component, ref DataBufferReader reader)
			{
				ghEntityToUEntity.TryGetValue(reader.ReadValue<GhGameEntitySafe>(), out component.Leader);
			}
		}

		public class Register : RegisterGameHostComponentData<HideoutLeaderSquad>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<HideoutLeaderSquad, Deserializer>();
		}
	}
}