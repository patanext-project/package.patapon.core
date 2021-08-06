using GameHost;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.Units
{
	public struct UnitEnemySeekingState : IComponentData, IValueDeserializer<UnitEnemySeekingState>
	{
		private struct Replica
		{
			public GhGameEntitySafe Enemy;
			public float            RelativeDistance;
			public float            SelfDistance;
		}

		public Entity Enemy;
		public float  RelativeDistance;
		public float  SelfDistance;

		public class Register : RegisterGameHostComponentData<UnitEnemySeekingState>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new CustomSingleDeserializer<UnitEnemySeekingState, UnitEnemySeekingState>();
		}

		public int Size => UnsafeUtility.SizeOf<Replica>();

		public void Deserialize(EntityManager em, NativeHashMap<GhGameEntitySafe, Entity> ghEntityToUEntity, ref UnitEnemySeekingState component, ref DataBufferReader reader)
		{
			var replica = reader.ReadValue<Replica>();
			RelativeDistance = replica.RelativeDistance;
			SelfDistance     = replica.SelfDistance;
			ghEntityToUEntity.TryGetValue(replica.Enemy, out Enemy);

			component = this;
		}
	}
}