using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public struct DefaultBackwardAbility : IComponentData, IReadWriteComponentSnapshot<DefaultBackwardAbility>, ISnapshotDelta<DefaultBackwardAbility>
	{
		public float AccelerationFactor;
		public float Delta;

		public struct Exclude : IComponentData
		{
		}

		public void WriteTo(DataStreamWriter writer, ref DefaultBackwardAbility baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedFloat(AccelerationFactor, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref DefaultBackwardAbility baseline, DeserializeClientData jobData)
		{
			AccelerationFactor = reader.ReadPackedFloat(ref ctx, jobData.NetworkCompressionModel);
		}

		public unsafe bool DidChange(DefaultBackwardAbility baseline)
		{
			fixed (void* addr = &this)
			{
				return UnsafeUtility.MemCmp(addr, &baseline, sizeof(DefaultBackwardAbility)) != 0;
			}
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<DefaultBackwardAbility>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	public class DefaultBackwardAbilityProvider : BaseRhythmAbilityProvider<DefaultBackwardAbility>
	{
		public const string MapPath = "backward_data";

		public override string MasterServerId  => nameof(P4OfficialAbilities.BasicBackward);
		public override Type   ChainingCommand => typeof(BackwardCommand);

		public override void SetEntityData(Entity entity, CreateAbility data)
		{
			base.SetEntityData(entity, data);
			EntityManager.SetComponentData(entity, GetValue(MapPath, new DefaultBackwardAbility
			{
				AccelerationFactor = 1
			}));
		}
	}
}