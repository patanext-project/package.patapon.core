using Patapon4TLB.Default;
using Patapon4TLB.Default.Player;
using Revolution;
using Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
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

		public float AttackMeleeRange;
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

			Attack      = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			AttackSpeed = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);

			Defense = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);

			MovementAttackSpeed = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			BaseWalkSpeed       = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
			FeverWalkSpeed      = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);

			Weight = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);

			AttackSeekRange = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
		}

		public struct Exclude : IComponentData
		{
		}

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

		public Entity SelfEnemy;
		public float3 SelfPosition;
		public float SelfDistance;

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
		public int Attack;
		public int Defense;

		public float ReceiveDamagePercentage;

		public float MovementSpeed;
		public float MovementAttackSpeed;
		public float MovementReturnSpeed;
		public float AttackSpeed;

		public float AttackSeekRange;

		public float Weight;

		public float GetAcceleration()
		{
			return math.clamp(math.rcp(Weight), 0, 1);
		}

		public class NetSynchronize : ComponentSnapshotSystemDelta<UnitPlayState, UnitPlayState.Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(ExcludeFromTagging);
		}

		public class LocalUpdate : ComponentUpdateSystemDirect<UnitPlayState, UnitPlayState.Snapshot>
		{
		}

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<UnitPlayState>
		{
			public UnitPlayState state;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedIntDelta(state.Attack, baseline.state.Attack, compressionModel);
				writer.WritePackedIntDelta(state.Defense, baseline.state.Defense, compressionModel);

				writer.WritePackedFloatDelta(state.ReceiveDamagePercentage, baseline.state.ReceiveDamagePercentage, compressionModel);

				writer.WritePackedFloatDelta(state.MovementSpeed, baseline.state.MovementSpeed, compressionModel);
				writer.WritePackedFloatDelta(state.MovementAttackSpeed, baseline.state.MovementAttackSpeed, compressionModel);
				writer.WritePackedFloatDelta(state.MovementReturnSpeed, baseline.state.MovementReturnSpeed, compressionModel);
				writer.WritePackedFloatDelta(state.AttackSpeed, baseline.state.AttackSpeed, compressionModel);

				writer.WritePackedFloatDelta(state.AttackSeekRange, baseline.state.AttackSeekRange, compressionModel);

				writer.WritePackedFloatDelta(state.Weight, baseline.state.Weight, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				state.Attack  = reader.ReadPackedIntDelta(ref ctx, baseline.state.Attack, compressionModel);
				state.Defense = reader.ReadPackedIntDelta(ref ctx, baseline.state.Defense, compressionModel);

				state.ReceiveDamagePercentage = reader.ReadPackedFloatDelta(ref ctx, baseline.state.ReceiveDamagePercentage, compressionModel);

				state.MovementSpeed       = reader.ReadPackedFloatDelta(ref ctx, baseline.state.MovementSpeed, compressionModel);
				state.MovementAttackSpeed = reader.ReadPackedFloatDelta(ref ctx, baseline.state.MovementAttackSpeed, compressionModel);
				state.MovementReturnSpeed = reader.ReadPackedFloatDelta(ref ctx, baseline.state.MovementReturnSpeed, compressionModel);
				state.AttackSpeed         = reader.ReadPackedFloatDelta(ref ctx, baseline.state.AttackSpeed, compressionModel);

				state.AttackSeekRange = reader.ReadPackedFloatDelta(ref ctx, baseline.state.AttackSeekRange, compressionModel);

				state.Weight = reader.ReadPackedFloatDelta(ref ctx, baseline.state.Weight, compressionModel);
			}

			public uint Tick { get; set; }

			public bool DidChange(Snapshot baseline)
			{
				baseline.Tick = Tick;
				return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
			}

			public void SynchronizeFrom(in UnitPlayState component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				state = component;
			}

			public void SynchronizeTo(ref UnitPlayState component, in DeserializeClientData deserializeData)
			{
				component = state;
			}
		}
	}

	public struct UnitDefinedAbilities : IBufferElementData
	{
		public NativeString512 Type;
		public int             Level;
		public AbilitySelection             Selection;

		public UnitDefinedAbilities(string type, int level, AbilitySelection selection = AbilitySelection.Horizontal)
		{
			Type      = new NativeString512(type);
			Level     = level;
			Selection = selection;
		}
	}
}