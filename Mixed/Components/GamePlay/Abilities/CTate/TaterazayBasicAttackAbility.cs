using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CTate
{
	public struct TaterazayBasicAttackAbility : IReadWriteComponentSnapshot<TaterazayBasicAttackAbility>, ISnapshotDelta<TaterazayBasicAttackAbility>
	{
		public const int DelaySlashMs = 150;

		public bool HasSlashed;

		public uint  AttackStartTick;
		public float NextAttackDelay;

		public class Provider : BaseRhythmAbilityProvider<TaterazayBasicAttackAbility>
		{
			public override string MasterServerId => nameof(P4OfficialAbilities.TateBasicAttack);
			public override Type ChainingCommand => typeof(AttackCommand);
			protected override string file_path_prefix => "tate";
		}

		public void WriteTo(DataStreamWriter writer, ref TaterazayBasicAttackAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUIntDelta(AttackStartTick, baseline.AttackStartTick, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref TaterazayBasicAttackAbility baseline, DeserializeClientData jobData)
		{
			AttackStartTick = reader.ReadPackedUIntDelta(ref ctx, baseline.AttackStartTick, jobData.NetworkCompressionModel);
		}

		public bool DidChange(TaterazayBasicAttackAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<TaterazayBasicAttackAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}