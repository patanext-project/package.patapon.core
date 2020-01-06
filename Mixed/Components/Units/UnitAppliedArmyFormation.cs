using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.Units
{
	// It could have been synchronized from real formation entities, but it would be too much cumbersome to setup and to synchronize through players.
	public struct UnitAppliedArmyFormation : IReadWriteComponentSnapshot<UnitAppliedArmyFormation>, ISnapshotDelta<UnitAppliedArmyFormation>
	{
		public int ArmyInFormation;
		public int IndexInArmy;

		public void WriteTo(DataStreamWriter writer, ref UnitAppliedArmyFormation baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedInt(ArmyInFormation, jobData.NetworkCompressionModel);
			writer.WritePackedInt(IndexInArmy, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref UnitAppliedArmyFormation baseline, DeserializeClientData jobData)
		{
			ArmyInFormation = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			IndexInArmy     = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(UnitAppliedArmyFormation baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<UnitAppliedArmyFormation>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}