using Patapon4TLB.Default;
using Revolution;
using Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.Units
{
	public struct UnitStatistics : IReadWriteComponentSnapshot<UnitStatistics>, ISnapshotDelta<UnitStatistics>
	{
		public int Health;

		public int   Attack;
		public float AttackSpeed;

		public int Defense;

		public float MovementAttackSpeed;
		public float BaseWalkSpeed;
		public float FeverWalkSpeed;

		/// <summary>
		///     Weight can be used to calculate unit acceleration for moving or for knock-back power amplification.
		/// </summary>
		public float Weight;

		public float AttackSeekRange;
		
		public void WriteTo(DataStreamWriter writer, ref UnitStatistics baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedInt(Health, jobData.NetworkCompressionModel);
			
			writer.WritePackedInt(Attack, jobData.NetworkCompressionModel);
			writer.WritePackedFloat(AttackSpeed, jobData.NetworkCompressionModel);
			
			writer.WritePackedInt(Defense, jobData.NetworkCompressionModel);
			
			writer.WritePackedFloat(MovementAttackSpeed, jobData.NetworkCompressionModel);
			writer.WritePackedFloat(BaseWalkSpeed, jobData.NetworkCompressionModel);
			writer.WritePackedFloat(FeverWalkSpeed, jobData.NetworkCompressionModel);
			
			writer.WritePackedFloat(Weight, jobData.NetworkCompressionModel);
			
			writer.WritePackedFloat(AttackSeekRange, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref UnitStatistics baseline, DeserializeClientData jobData)
		{
			Health = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			
			Attack = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			AttackSpeed = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			
			Defense = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			
			MovementAttackSpeed = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			BaseWalkSpeed = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			FeverWalkSpeed = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			
			Weight = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			
			AttackSeekRange = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
		}
		
		public struct Exclude : IComponentData {}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<UnitStatistics>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public bool DidChange(UnitStatistics baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}
	}

	public struct UnitStatusEffectStatistics : IBufferElementData
	{
		public StatusEffect Type;
		public int          CustomType; // this value would be set only if 'Type' is set to Unknown

		public float Value;
		public float RegenPerSecond;
	}

	public struct UnitEnemySeekingState : IReadWriteComponentSnapshot<UnitEnemySeekingState, GhostSetup>, ISnapshotDelta<UnitEnemySeekingState>
	{
		public Entity Enemy;
		public float  Distance;

		public void WriteTo(DataStreamWriter writer, ref UnitEnemySeekingState baseline, GhostSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt(setup[Enemy], jobData.NetworkCompressionModel);
			writer.WritePackedFloat(Distance, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref UnitEnemySeekingState baseline, DeserializeClientData jobData)
		{
			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out Enemy);
			Distance = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
		}

		public bool DidChange(UnitEnemySeekingState baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<UnitEnemySeekingState, GhostSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	public struct UnitPlayState : IComponentData
	{
		public float MovementSpeed;
		public float MovementAttackSpeed;
		public float AttackSpeed;

		public float AttackSeekRange;

		public float Weight; // not used for now

		public class NetSynchronize : ComponentSnapshotSystemEmpty<UnitPlayState>
		{
			public override ComponentType ExcludeComponent => typeof(ExcludeFromTagging);
		}
	}

	public struct UnitDefinedAbilities : IBufferElementData
	{
		public NativeString512 Type;
		public int             Level;

		public UnitDefinedAbilities(string type, int level)
		{
			Type  = new NativeString512(type);
			Level = level;
		}
	}
}