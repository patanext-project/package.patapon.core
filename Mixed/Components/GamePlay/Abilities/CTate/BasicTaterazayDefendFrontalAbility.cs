using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CTate
{
	public struct BasicTaterazayDefendFrontalAbility : IReadWriteComponentSnapshot<BasicTaterazayDefendFrontalAbility>, ISnapshotDelta<BasicTaterazayDefendFrontalAbility>
	{
		// 10 is a good range
		public float Range;
		
		public class Provider : BaseRhythmAbilityProvider<BasicTaterazayDefendFrontalAbility>
		{

		}

		public void WriteTo(DataStreamWriter writer, ref BasicTaterazayDefendFrontalAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref BasicTaterazayDefendFrontalAbility baseline, DeserializeClientData jobData)
		{
		}

		public bool DidChange(BasicTaterazayDefendFrontalAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<BasicTaterazayDefendFrontalAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}