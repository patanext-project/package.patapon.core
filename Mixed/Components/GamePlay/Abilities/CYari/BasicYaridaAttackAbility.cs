using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CYari
{
	public struct BasicYaridaAttackAbility : IReadWriteComponentSnapshot<BasicYaridaAttackAbility>, ISnapshotDelta<BasicYaridaAttackAbility>
	{
		public const uint DelayThrowMs = 300;
		
		public uint AttackStartTick;
		public float NextAttackDelay;
		public bool HasThrown;
		
		public float ThrowSpeed;
		public float ThrowHeight;

		public class Provider : BaseRhythmAbilityProvider<BasicYaridaAttackAbility>
		{
			public override void SetEntityData(Entity entity, Create data)
			{
				base.SetEntityData(entity, data);
				EntityManager.SetComponentData(entity, new BasicYaridaAttackAbility {ThrowSpeed = 10f, ThrowHeight = 10f});
			}
		}

		public void WriteTo(DataStreamWriter writer, ref BasicYaridaAttackAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt(AttackStartTick, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref BasicYaridaAttackAbility baseline, DeserializeClientData jobData)
		{
			AttackStartTick = reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(BasicYaridaAttackAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<BasicYaridaAttackAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}