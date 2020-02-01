using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CYari
{
	public struct YaridaLeapSpearAbility : IReadWriteComponentSnapshot<YaridaLeapSpearAbility>, ISnapshotDelta<YaridaLeapSpearAbility>
	{
		public const uint DelayThrowMs = 500;
		
		public uint AttackStartTick;
		public float NextAttackDelay;
		public bool HasThrown;

		public float2 ThrowVec;

		public class Provider : BaseRhythmAbilityProvider<YaridaLeapSpearAbility>
		{
			public override string MasterServerId  => nameof(P4OfficialAbilities.YariLeapSpear);
			public override Type   ChainingCommand => typeof(AttackCommand);
			protected override string file_path_prefix => "yari";
			
			public override void SetEntityData(Entity entity, CreateAbility data)
			{
				base.SetEntityData(entity, data);
				EntityManager.SetComponentData(entity, new YaridaLeapSpearAbility {ThrowVec = new float2(22.5f, -8f)});
			}
		}

		public void WriteTo(DataStreamWriter writer, ref YaridaLeapSpearAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt(AttackStartTick, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref YaridaLeapSpearAbility baseline, DeserializeClientData jobData)
		{
			AttackStartTick = reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(YaridaLeapSpearAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<YaridaLeapSpearAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}