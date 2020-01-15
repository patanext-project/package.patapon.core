using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CTate
{
	public struct BasicTaterazayDefendAbility : IReadWriteComponentSnapshot<BasicTaterazayDefendAbility>, ISnapshotDelta<BasicTaterazayDefendAbility>
	{
		public int foo;

		public class Provider : BaseRhythmAbilityProvider<BasicTaterazayDefendAbility>
		{

		}

		public void WriteTo(DataStreamWriter writer, ref BasicTaterazayDefendAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref BasicTaterazayDefendAbility baseline, DeserializeClientData jobData)
		{
		}

		public bool DidChange(BasicTaterazayDefendAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<BasicTaterazayDefendAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}