using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.Units
{
	public struct UnitDirection : IReadWriteComponentSnapshot<UnitDirection>
	{
		public static readonly UnitDirection Right = new UnitDirection {Value = 1};
		public static readonly UnitDirection Left  = new UnitDirection {Value = -1};

		public sbyte Value;

		public bool IsLeft  => Value == -1;
		public bool IsRight => Value == 1;

		public bool Invalid => !IsLeft && !IsRight;

		public void WriteTo(DataStreamWriter writer, ref UnitDirection baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WriteBitBool(IsLeft);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref UnitDirection baseline, DeserializeClientData jobData)
		{
			Value = (sbyte) (reader.ReadBitBool(ref ctx) ? -1 : 1);
		}

		public bool Equals(UnitDirection other)
		{
			return Value == other.Value;
		}

		// delta system worth it?
		public class NetSynchronize : MixedComponentSnapshotSystem<UnitDirection, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public struct Exclude : IComponentData
		{
		}
	}
}