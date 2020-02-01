using System;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon4TLB.Default.Player;
using Revolution;
using Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public struct OwnerActiveAbility : IComponentData
	{
		public int LastCommandActiveTime;
		public int LastActivationTime;

		public Entity Active;
		public Entity Incoming;

		/// <summary>
		/// Current combo of the entity...
		/// </summary>
		public FixedList32<Entity> CurrentCombo; //< 32 bytes should suffice, it would be 4 combo commands...

		public void AddCombo(Entity ent)
		{
			while (CurrentCombo.Length >= CurrentCombo.Capacity)
				CurrentCombo.RemoveAt(0);
			CurrentCombo.Add(ent);
		}

		public bool RemoveCombo(Entity ent)
		{
			var index = CurrentCombo.IndexOf(ent);
			if (index < 0)
				return false;
			CurrentCombo.RemoveAt(index);
			return true;
		}

		public class NetEmptySynchronize : ComponentSnapshotSystemTag<OwnerActiveAbility>
		{
		}
	}

	public struct AbilityState : IComponentData
	{
		public EAbilityPhase Phase;

		public int UpdateVersion;
		public int ActivationVersion;

		public class NetEmptySynchronize : ComponentSnapshotSystemTag<AbilityState>
		{
		}
	}

	[Flags]
	public enum EAbilityPhase
	{
		None             = 0,
		WillBeActive     = 1 << 0,
		Active           = 1 << 1,
		Chaining         = 1 << 2,
		ActiveOrChaining = Active | Chaining,
	}

	public struct AbilityActivation : IComponentData, IReadWriteComponentSnapshot<AbilityActivation, GhostSetup>, ISnapshotDelta<AbilityActivation>
	{
		public AbilitySelection Selection;

		/// <summary>
		/// The command used for chaining.
		/// </summary>
		public Entity Chaining;

		/// <summary>
		/// Combo command list, excluding the chaining command.
		/// </summary>
		public FixedList32<Entity> Combos; //< 32 bytes should suffice, it would be 4 combo commands...

		public void WriteTo(DataStreamWriter writer, ref AbilityActivation baseline, GhostSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt(setup[Chaining], jobData.NetworkCompressionModel);
			writer.WritePackedUInt((uint) Combos.Length, jobData.NetworkCompressionModel);
			for (var i = 0; i != Combos.Length; i++)
				writer.WritePackedUInt(setup[Combos[i]], jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref AbilityActivation baseline, DeserializeClientData jobData)
		{
			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out Chaining);
			Combos.Length = (int) reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
			for (var i = 0; i != Combos.Length; i++)
				jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out Combos[i]);
		}

		public bool DidChange(AbilityActivation baseline)
		{
			return UnsafeUtilityOp.AreNotEquals(ref this, ref baseline);
		}

		public struct Exclude : IComponentData
		{
		}

		public class NetSynchronize : MixedComponentSnapshotSystemDelta<AbilityActivation, GhostSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}

	public struct AbilityEngineSet : IComponentData
	{
		public Entity Engine;

		public FlowEngineProcess    Process;
		public RhythmEngineSettings Settings;
		public RhythmCurrentCommand CurrentCommand;
		public GameComboState       ComboState;
		public GameCommandState     CommandState;

		public Entity         Command, PreviousCommand;
		public GameComboState Combo,   PreviousCombo;

		public class NetEmptySynchronize : ComponentSnapshotSystemTag<AbilityEngineSet>
		{
		}
	}
}