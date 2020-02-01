using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using Scripts.Utilities;
using StormiumTeam.GameBase;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public struct DefaultPartyAbility : IComponentData
	{
		public bool WasActive;

		public UTimeProgression Progression;
		public int              EnergyOnActivation;
		public int              TickPerSecond;
		public int              EnergyPerTick;

		public struct Exclude : IComponentData
		{
		}

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISnapshotDelta<Snapshot>, ISynchronizeImpl<DefaultPartyAbility>
		{
			public int EnergyOnActivation;
			public int TickPerSecond;
			public int EnergyPerTick;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedIntDelta(EnergyOnActivation, baseline.EnergyOnActivation, compressionModel);
				writer.WritePackedIntDelta(TickPerSecond, baseline.TickPerSecond, compressionModel);
				writer.WritePackedIntDelta(EnergyPerTick, baseline.EnergyPerTick, compressionModel);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				EnergyOnActivation = reader.ReadPackedIntDelta(ref ctx, baseline.EnergyOnActivation, compressionModel);
				TickPerSecond      = reader.ReadPackedIntDelta(ref ctx, baseline.TickPerSecond, compressionModel);
				EnergyPerTick      = reader.ReadPackedIntDelta(ref ctx, baseline.EnergyPerTick, compressionModel);
			}

			public uint Tick { get; set; }

			public unsafe bool DidChange(Snapshot baseline)
			{
				baseline.Tick = Tick;
				return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
			}

			public void SynchronizeFrom(in DefaultPartyAbility component, in DefaultSetup setup, in SerializeClientData serializeData)
			{
				EnergyOnActivation = component.EnergyOnActivation;
				TickPerSecond      = component.TickPerSecond;
				EnergyPerTick      = component.EnergyPerTick;
			}

			public void SynchronizeTo(ref DefaultPartyAbility component, in DeserializeClientData deserializeData)
			{
				component.EnergyOnActivation = EnergyOnActivation;
				component.TickPerSecond      = TickPerSecond;
				component.EnergyPerTick      = EnergyPerTick;
			}
		}

		public class NetSynchronize : ComponentSnapshotSystemDelta<DefaultPartyAbility, Snapshot>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public class LocalUpdate : ComponentUpdateSystemDirect<DefaultPartyAbility, Snapshot>
		{
		}
	}

	public class DefaultPartyAbilityProvider : BaseRhythmAbilityProvider<DefaultPartyAbility>
	{
		public const string MapPath = "party_data";

		public override string MasterServerId  => nameof(P4OfficialAbilities.BasicParty);
		public override Type   ChainingCommand => typeof(PartyCommand);

		public override void SetEntityData(Entity entity, CreateAbility data)
		{
			base.SetEntityData(entity, data);
			EntityManager.SetComponentData(entity, GetValue(MapPath, new DefaultPartyAbility
			{
				TickPerSecond      = 100,
				EnergyPerTick      = 1,
				EnergyOnActivation = 30
			}));
		}
	}
}