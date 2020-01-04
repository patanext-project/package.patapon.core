using Revolution;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public struct DefaultMarchAbility : IComponentData
	{
		public float AccelerationFactor;
		public float Delta;

		public struct Exclude : IComponentData
		{
		}

		public unsafe struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<DefaultMarchAbility>
		{
			public float AccelerationFactor;
			public float Delta;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedFloatDelta(AccelerationFactor, baseline.AccelerationFactor, compressionModel);
				writer.WritePackedFloatDelta(Delta, baseline.Delta, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				AccelerationFactor = reader.ReadPackedFloatDelta(ref ctx, baseline.AccelerationFactor, compressionModel);
				Delta              = reader.ReadPackedFloatDelta(ref ctx, baseline.Delta, compressionModel);
			}

			public uint Tick { get; set; }

			public bool DidChange(Snapshot baseline)
			{
				fixed (void* addr = &this)
				{
					return UnsafeUtility.MemCmp(addr, &baseline, sizeof(Snapshot)) != 0;
				}
			}

			public void SynchronizeFrom(in DefaultMarchAbility component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				AccelerationFactor = component.AccelerationFactor;
				Delta              = component.Delta;
			}

			public void SynchronizeTo(ref DefaultMarchAbility component, in DeserializeClientData deserializeData)
			{
				component.AccelerationFactor = AccelerationFactor;
				component.Delta              = Delta;
			}
		}

		public class NetSynchronize : ComponentSnapshotSystemDelta<DefaultMarchAbility, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class LocalUpdate : ComponentUpdateSystemDirect<DefaultMarchAbility, Snapshot>
		{
		}
	}

	public class DefaultMarchAbilityProvider : BaseRhythmAbilityProvider<DefaultMarchAbility>
	{
	}
}