using Revolution;
using Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Utilities;

namespace Patapon.Mixed.Units
{
	/// <summary>
	/// ECS representation of unit equipment list
	/// todo: It should be done in a different way...
	/// </summary>
	public struct UnitDisplayedEquipment : IComponentData
	{
		public NativeString64 Mask;
		public NativeString64 LeftEquipment;
		public NativeString64 RightEquipment;

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<UnitDisplayedEquipment>
		{
			public NativeString64 Mask;
			public NativeString64 LeftArm;
			public NativeString64 RightArm;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedStringDelta(Mask, baseline.Mask, compressionModel);
				writer.WritePackedStringDelta(LeftArm, baseline.LeftArm, compressionModel);
				writer.WritePackedStringDelta(RightArm, baseline.RightArm, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Mask     = reader.ReadPackedStringDelta(ref ctx, baseline.Mask, compressionModel);
				LeftArm  = reader.ReadPackedStringDelta(ref ctx, baseline.LeftArm, compressionModel);
				RightArm = reader.ReadPackedStringDelta(ref ctx, baseline.RightArm, compressionModel);
			}

			public uint Tick { get; set; }

			public bool DidChange(Snapshot baseline)
			{
				baseline.Tick = Tick;
				return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
			}

			public void SynchronizeFrom(in UnitDisplayedEquipment component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				this.Mask     = component.Mask;
				this.LeftArm  = component.LeftEquipment;
				this.RightArm = component.RightEquipment;
			}

			public void SynchronizeTo(ref UnitDisplayedEquipment component, in DeserializeClientData deserializeData)
			{
				component.Mask     = Mask;
				component.LeftEquipment  = LeftArm;
				component.RightEquipment = RightArm;
			}
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : ComponentSnapshotSystemDelta<UnitDisplayedEquipment, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class LocalUpdate : ComponentUpdateSystemDirect<UnitDisplayedEquipment, Snapshot>
		{
		}
	}
}