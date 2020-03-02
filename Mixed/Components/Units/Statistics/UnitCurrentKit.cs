using System;
using P4TLB.MasterServer;
using Revolution;
using Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Utilities;

namespace Patapon.Mixed.Units.Statistics
{
	// Everyone will call the kit as classes, but here this is just for differentiating the Class 'object' type and Class 'unit type' word.
	public struct UnitCurrentKit : IReadWriteComponentSnapshot<UnitCurrentKit>, ISnapshotDelta<UnitCurrentKit>
	{
		public NativeString64 Value;

		public void WriteTo(DataStreamWriter writer, ref UnitCurrentKit baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedStringDelta(Value, default, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref UnitCurrentKit baseline, DeserializeClientData jobData)
		{
			Value = reader.ReadPackedStringDelta(ref ctx, default(NativeString64), jobData.NetworkCompressionModel);
		}

		public bool DidChange(UnitCurrentKit baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<UnitCurrentKit>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	public static class UnitKnownTypes
	{
		public static readonly NativeString64 Taterazay = new NativeString64("taterazay");
		public static readonly NativeString64 Yarida    = new NativeString64("yarida");
		public static readonly NativeString64 Yumiyacha = new NativeString64("yumiyacha");
		public static readonly NativeString64 Kibadda = new NativeString64("kibadda");
		public static readonly NativeString64 Pingrek = new NativeString64("pingrek");

		public static NativeString64 FromEnum(P4OfficialKit kit)
		{
			switch (kit)
			{
				case P4OfficialKit.Taterazay:
					return Taterazay;
				case P4OfficialKit.Yarida:
					return Yarida;
				case P4OfficialKit.Yumiyacha:
					return Yumiyacha;
				case P4OfficialKit.Kibadda:
					return Kibadda;
				case P4OfficialKit.Pingrek:
					return Pingrek;
				default:
					throw new ArgumentOutOfRangeException(nameof(kit), kit, null);
			}

			return default;
		}
	}
}