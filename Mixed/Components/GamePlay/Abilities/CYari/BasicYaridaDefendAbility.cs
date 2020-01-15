using Revolution;
using Scripts.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities.CYari
{
	public struct BasicYaridaDefendAbility : IReadWriteComponentSnapshot<BasicYaridaDefendAbility>, ISnapshotDelta<BasicYaridaDefendAbility>
	{
		public const uint DelayThrowMs = 300;

		public uint  AttackStartTick;
		public float NextAttackDelay;
		public bool  HasThrown;

		public float2 ThrowVec;

		public class Provider : BaseRhythmAbilityProvider<BasicYaridaDefendAbility>
		{
			public override void SetEntityData(Entity entity, Create data)
			{
				base.SetEntityData(entity, data);
				EntityManager.SetComponentData(entity, new BasicYaridaDefendAbility {ThrowVec = {x = 12.5f, y = 0}});
			}
		}

		public void WriteTo(DataStreamWriter writer, ref BasicYaridaDefendAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt(AttackStartTick, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref BasicYaridaDefendAbility baseline, DeserializeClientData jobData)
		{
			AttackStartTick = reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(BasicYaridaDefendAbility baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<BasicYaridaDefendAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}