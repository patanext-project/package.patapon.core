using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.Units
{
	// It could have been synchronized from real formation entities, but it would be too much cumbersome to setup and to synchronize through players.
	public struct UnitAppliedArmyFormation : IReadWriteComponentSnapshot<UnitAppliedArmyFormation>, ISnapshotDelta<UnitAppliedArmyFormation>
	{
		public int FormationIndex;
		public int ArmyIndex;
		public int IndexInFormation;

		public void WriteTo(DataStreamWriter writer, ref UnitAppliedArmyFormation baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedInt(FormationIndex, jobData.NetworkCompressionModel);
			writer.WritePackedInt(ArmyIndex, jobData.NetworkCompressionModel);
			writer.WritePackedInt(IndexInFormation, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref UnitAppliedArmyFormation baseline, DeserializeClientData jobData)
		{
			FormationIndex   = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			ArmyIndex        = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			IndexInFormation = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
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