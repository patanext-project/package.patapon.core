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
	public struct OwnerActiveAbility : IComponentData, IReadWriteComponentSnapshot<OwnerActiveAbility, GhostSetup>
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
		
		public struct Exclude : IComponentData {}

		/*public class NetEmptySynchronize : MixedComponentSnapshotSystem<OwnerActiveAbility, GhostSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}*/

		public class NetEmptySynchronizer : ComponentSnapshotSystemTag<OwnerActiveAbility>
		{
		}

		public void WriteTo(DataStreamWriter writer, ref OwnerActiveAbility baseline, GhostSetup setup, SerializeClientData jobData)
		{
			
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref OwnerActiveAbility baseline, DeserializeClientData jobData)
		{
			
		}
	}

	public struct AbilityState : IComponentData
	{
		public EAbilityPhase Phase;

		public int Combo;
		public int ImperfectCountWhileActive;

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
		/// <summary>
		/// This state is used when the Hero mode is getting activated since they do possess a delay of a beat...
		/// </summary>
		HeroActivation = 1 << 3,
	}

	public enum EActivationType
	{
		Normal,
		HeroMode
	}

	public struct AbilityActivation : IComponentData, IReadWriteComponentSnapshot<AbilityActivation, GhostSetup>, ISnapshotDelta<AbilityActivation>
	{
		public EActivationType Type;
		public int             HeroModeMaxCombo;
		public int             HeroModeImperfectLimitBeforeDeactivation;

		public AbilitySelection Selection;

		/// <summary>
		/// The command used for chaining.
		/// </summary>
		public Entity Chaining;

		/// <summary>
		/// Combo command list, excluding the chaining command.
		/// </summary>
		public FixedList32<Entity> Combos; //< 32 bytes should suffice, it would be 4 combo commands...

		/// <summary>
		/// Allowed commands for chaining in hero mode.
		/// </summary>
		public FixedList64<Entity> HeroModeAllowedCommands; //< 64 bytes should suffice, it would be up to 8 commands...

		public void WriteTo(DataStreamWriter writer, ref AbilityActivation baseline, GhostSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUInt((uint) Type, jobData.NetworkCompressionModel);
			writer.WritePackedInt(HeroModeMaxCombo, jobData.NetworkCompressionModel);
			writer.WritePackedInt(HeroModeImperfectLimitBeforeDeactivation, jobData.NetworkCompressionModel);

			writer.WritePackedUInt(setup[Chaining], jobData.NetworkCompressionModel);
			writer.WritePackedUInt((uint) Combos.Length, jobData.NetworkCompressionModel);
			writer.WritePackedUInt((uint) HeroModeAllowedCommands.Length, jobData.NetworkCompressionModel);

			for (var i = 0; i != Combos.Length; i++)
				writer.WritePackedUInt(setup[Combos[i]], jobData.NetworkCompressionModel);
			for (var i = 0; i != HeroModeAllowedCommands.Length; i++)
				writer.WritePackedUInt(setup[HeroModeAllowedCommands[i]], jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref AbilityActivation baseline, DeserializeClientData jobData)
		{
			Type                                     = (EActivationType) reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
			HeroModeMaxCombo                         = (int) reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			HeroModeImperfectLimitBeforeDeactivation = (int) reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);

			jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out Chaining);
			Combos.Length                  = (int) reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);
			HeroModeAllowedCommands.Length = (int) reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel);

			for (var i = 0; i != Combos.Length; i++)
				jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out Combos[i]);
			for (var i = 0; i != HeroModeAllowedCommands.Length; i++)
				jobData.GhostToEntityMap.TryGetValue(reader.ReadPackedUInt(ref ctx, jobData.NetworkCompressionModel), out HeroModeAllowedCommands[i]);
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