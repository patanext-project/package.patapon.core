using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CYari
{
	public struct JumpYaridaAttackAbility : IReadWriteComponentSnapshot<JumpYaridaAttackAbility>, ISnapshotDelta<JumpYaridaAttackAbility>
	{
		public const uint DelayThrowMs = 450;
		
		public uint AttackStartTick;
		public float NextAttackDelay;
		public bool HasThrown;

		public float2 ThrowVec;

		public class Provider : BaseRhythmAbilityProvider<JumpYaridaAttackAbility>
		{
			public override void SetEntityData(Entity entity, Create data)
			{
				base.SetEntityData(entity, data);
				EntityManager.SetComponentData(entity, new JumpYaridaAttackAbility {ThrowVec = new float2(20f, -6f)});
			}
		}

		public void WriteTo(DataStreamWriter writer, ref JumpYaridaAttackAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt(AttackStartTick, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref JumpYaridaAttackAbility baseline, DeserializeClientData jobData)
		{
			AttackStartTick = reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(JumpYaridaAttackAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<JumpYaridaAttackAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}