using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.Units
{
	public struct RhythmCurrentCommand : IComponentData
	{
		public Entity Previous;
		public Entity CommandTarget;

		/// <summary>
		///     When will the command be active?
		/// </summary>
		/// <remarks>
		///     >=0 = the active beat (will have the same effect as -1 if CommandTarget don't exist or is null).
		///     -1 = not in effect.
		///     -2 = forever.
		/// </remarks>
		public int ActiveAtTime;

		/// <summary>
		///     If you want to set a custom beat ending.
		/// </summary>
		/// <remarks>
		///     >0 = the ending beat.
		///     0 = the command will never be executed (but why).
		///     -1 = not in effect.
		///     -2 = forever (you can make a combo with ActiveAtBeat set at -1 to have a forever non ending command).
		/// </remarks>
		public int CustomEndTime;

		/// <summary>
		///     Power is associated with beat score, this is a value between 0 and 100.
		/// </summary>
		/// <remarks>
		///     This is not associated at all with fever state, the command will check if there is fever or not on the engine.
		///     The game will check if it can enable hero mode if power is 100.
		/// </remarks>
		public int Power;

		public bool HasPredictedCommands;

		public struct Exclude : IComponentData
		{
		}

		public class Synchronize : ComponentSnapshotSystemDelta<RhythmCurrentCommand, Snapshot, GhostSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}

		public struct Snapshot : IReadWriteSnapshot<Snapshot>, ISynchronizeImpl<RhythmCurrentCommand, GhostSetup>, ISnapshotDelta<Snapshot>
		{
			public uint CommandTarget;
			public int  ActiveAtTime;
			public int  CustomEndTime;
			public int  Power;
			public bool HasPredictedCommand;

			public void WriteTo(DataStreamWriter writer, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				writer.WritePackedUIntDelta(CommandTarget, baseline.CommandTarget, compressionModel);
				writer.WritePackedIntDelta(ActiveAtTime, baseline.ActiveAtTime, compressionModel);
				writer.WritePackedIntDelta(CustomEndTime, baseline.CustomEndTime, compressionModel);
				writer.WritePackedIntDelta(Power, baseline.Power, compressionModel);
				writer.WriteBitBool(HasPredictedCommand);
			}

			public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref Snapshot baseline, NetworkCompressionModel compressionModel)
			{
				CommandTarget       = reader.ReadPackedUIntDelta(ref ctx, baseline.CommandTarget, compressionModel);
				ActiveAtTime        = reader.ReadPackedIntDelta(ref ctx, baseline.ActiveAtTime, compressionModel);
				CustomEndTime       = reader.ReadPackedIntDelta(ref ctx, baseline.CustomEndTime, compressionModel);
				Power               = reader.ReadPackedIntDelta(ref ctx, baseline.Power, compressionModel);
				HasPredictedCommand = reader.ReadBitBool(ref ctx);
			}

			public uint Tick { get; set; }

			public void SynchronizeFrom(in RhythmCurrentCommand component, in GhostSetup setup, in SerializeClientData serializeData)
			{
				CommandTarget       = setup[component.CommandTarget];
				ActiveAtTime        = component.ActiveAtTime;
				CustomEndTime       = component.CustomEndTime;
				Power               = component.Power;
				HasPredictedCommand = component.HasPredictedCommands;
			}

			public void SynchronizeTo(ref RhythmCurrentCommand component, in DeserializeClientData deserializeData)
			{
				deserializeData.GhostToEntityMap.TryGetValue(CommandTarget, out component.CommandTarget);
				component.ActiveAtTime         = ActiveAtTime;
				component.CustomEndTime        = CustomEndTime;
				component.Power                = Power;
				component.HasPredictedCommands = HasPredictedCommand;
			}

			public bool DidChange(Snapshot baseline)
			{
				return CommandTarget != baseline.CommandTarget
				       || ActiveAtTime != baseline.ActiveAtTime
				       || CustomEndTime != baseline.CustomEndTime
				       || Power != baseline.Power
				       || HasPredictedCommand != baseline.HasPredictedCommand;
			}
		}
	}
}