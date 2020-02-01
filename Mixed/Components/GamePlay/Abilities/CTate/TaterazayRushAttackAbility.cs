using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CTate
{
	public struct TaterazayRushAttackAbility : IReadWriteComponentSnapshot<TaterazayRushAttackAbility>, ISnapshotDelta<TaterazayRushAttackAbility>
	{
		public const int AttackDelayMs = 50;

		public enum EPhase
		{
			/// <summary>
			/// Recover from previous attack...
			/// </summary>
			Wait,

			/// <summary>
			/// Run to the enemy
			/// </summary>
			Run,

			/// <summary>
			/// An attack is being requested
			/// </summary>
			AttackRequested,
			
			/// <summary>
			/// We are currently attacking
			/// </summary>
			Attacking,
		}

		public EPhase Phase;

		public uint  AttackStartTick;
		public uint  ForceAttackTick;
		public float NextAttackDelay;

		public class Provider : BaseRhythmAbilityProvider<TaterazayRushAttackAbility>
		{
			public override    string MasterServerId   => nameof(P4OfficialAbilities.TateRushAttack);
			public override    Type   ChainingCommand  => typeof(AttackCommand);
			public override    Type[] ComboCommands    => new[] {typeof(ChargeCommand)};
			protected override string file_path_prefix => "tate";
		}

		public void WriteTo(DataStreamWriter writer, ref TaterazayRushAttackAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUIntDelta(AttackStartTick, baseline.AttackStartTick, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref TaterazayRushAttackAbility baseline, DeserializeClientData jobData)
		{
			AttackStartTick = reader.ReadPackedUIntDelta(ref ctx, baseline.AttackStartTick, jobData.NetworkCompressionModel);
		}

		public bool DidChange(TaterazayRushAttackAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<TaterazayRushAttackAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}