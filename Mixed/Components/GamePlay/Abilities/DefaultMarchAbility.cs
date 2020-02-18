using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public struct DefaultSubsetMarch : IComponentData
	{
		[Flags]
		public enum ESubSet
		{
			None     = 0,
			Cursor   = 1 << 1,
			Movement = 1 << 2,
			All      = Cursor | Movement
		}

		public bool    IsActive;
		public ESubSet SubSet;

		public float AccelerationFactor;
		public float Delta;
	}

	public struct DefaultMarchAbility : IComponentData
	{
		public float AccelerationFactor;

		public struct Exclude : IComponentData
		{
		}

		public unsafe struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<DefaultMarchAbility>
		{
			public float AccelerationFactor;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedFloatDelta(AccelerationFactor, baseline.AccelerationFactor, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				AccelerationFactor = reader.ReadPackedFloatDelta(ref ctx, baseline.AccelerationFactor, compressionModel);
			}

			public uint Tick { get; set; }

			public bool DidChange(Snapshot baseline)
			{
				fixed (void* addr = &this)
				{
					return UnsafeUtility.MemCmp(addr, &baseline, sizeof(Snapshot)) != 0;
				}
			}

			public void SynchronizeFrom(in DefaultMarchAbility component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				AccelerationFactor = component.AccelerationFactor;
			}

			public void SynchronizeTo(ref DefaultMarchAbility component, in DeserializeClientData deserializeData)
			{
				component.AccelerationFactor = AccelerationFactor;
			}
		}

		public class NetSynchronize : ComponentSnapshotSystemDelta<DefaultMarchAbility, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class LocalUpdate : ComponentUpdateSystemDirect<DefaultMarchAbility, Snapshot>
		{
		}
	}

	public class DefaultMarchAbilityProvider : BaseRhythmAbilityProvider<DefaultMarchAbility>
	{
		public const string MapPath = "march_data";

		public override string MasterServerId  => nameof(P4OfficialAbilities.BasicMarch);
		public override Type   ChainingCommand => typeof(MarchCommand);

		public override void SetEntityData(Entity entity, CreateAbility data)
		{
			base.SetEntityData(entity, data);
			EntityManager.SetComponentData(entity, GetValue(MapPath, new DefaultMarchAbility
			{
				AccelerationFactor = 1
			}));
			EntityManager.AddComponentData(entity, new DefaultSubsetMarch
			{
				SubSet             = DefaultSubsetMarch.ESubSet.All,
				AccelerationFactor = 1
			});
		}
	}
}