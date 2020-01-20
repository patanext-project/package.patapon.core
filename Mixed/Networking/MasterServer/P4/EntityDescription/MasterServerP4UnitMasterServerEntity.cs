using package.stormiumteam.shared;
using Revolution;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Utilities;

namespace Patapon4TLB.Core.MasterServer.P4.EntityDescription
{
	public struct MasterServerP4UnitMasterServerEntity : IMasterServerEntityDescription<MasterServerP4UnitMasterServerEntity>
	{
		public ulong UnitId;

		public bool Equals(MasterServerP4UnitMasterServerEntity other)
		{
			return UnitId == other.UnitId;
		}

		public override bool Equals(object obj)
		{
			return obj is MasterServerP4UnitMasterServerEntity other && Equals(other);
		}

		public override int GetHashCode()
		{
			return UnitId.GetHashCode();
		}

		public struct Exclude : IComponentData
		{
		}

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISynchronizeImpl<MasterServerP4UnitMasterServerEntity>, ISnapshotDelta<Snapshot>
		{
			public ulong Id;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedULongDelta(Id, baseline.Id, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				Id = reader.ReadPackedULongDelta(ref ctx, baseline.Id, compressionModel);
			}

			public uint Tick { get; set; }

			public void SynchronizeFrom(in MasterServerP4UnitMasterServerEntity component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				Id = component.UnitId;
			}

			public void SynchronizeTo(ref MasterServerP4UnitMasterServerEntity component, in DeserializeClientData deserializeData)
			{
				component.UnitId = Id;
			}

			public bool DidChange(Snapshot baseline)
			{
				return Id != baseline.Id;
			}
		}

		public class NetSynchronize : ComponentSnapshotSystemDelta<MasterServerP4UnitMasterServerEntity, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class LocalUpdate : ComponentUpdateSystemDirect<MasterServerP4UnitMasterServerEntity, Snapshot>
		{
		}
	}
}