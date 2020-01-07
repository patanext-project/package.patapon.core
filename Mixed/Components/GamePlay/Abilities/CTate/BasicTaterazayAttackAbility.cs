using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CTate
{
	public struct BasicTaterazayAttackAbility : IReadWriteComponentSnapshot<BasicTaterazayAttackAbility>, ISnapshotDelta<BasicTaterazayAttackAbility>
	{
		public const int DelaySlashMs = 100;

		public bool HasSlashed;

		public uint  AttackStartTick;
		public float NextAttackDelay;

		public class Provider : BaseRhythmAbilityProvider<BasicTaterazayAttackAbility>
		{

		}

		public void WriteTo(DataStreamWriter writer, ref BasicTaterazayAttackAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUIntDelta(AttackStartTick, baseline.AttackStartTick, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref BasicTaterazayAttackAbility baseline, DeserializeClientData jobData)
		{
			AttackStartTick = reader.ReadPackedUIntDelta(ref ctx, baseline.AttackStartTick, jobData.NetworkCompressionModel);
		}

		public bool DidChange(BasicTaterazayAttackAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<BasicTaterazayAttackAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}