using package.stormiumteam.shared;
using Unity.Mathematics;
using Revolution.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default.Snapshot
{
	public struct RhythmEngineSnapshotData : ISnapshotData<RhythmEngineSnapshotData>
	{
		public uint Tick { get; set; }

		public bool IsPaused;
		public bool UseClientSimulation;
		public uint MaxBeats;
		public uint BeatInterval;

		public int  OwnerGhostId;
		public int StartTime;     // in ms
		public int  CommandTypeId; // CommandState.IsActive will be set if ghostId is null or not
		public int CommandStartTime;
		public int CommandEndTime;
		public int CommandChainEndTime;
		public int  Recovery;

		public int  ComboScore;
		public uint  ComboChain;
		public uint  ComboChainToFever;
		public uint  ComboJinnEnergy;
		public uint  ComboJinnEnergyMax;
		public bool ComboIsFever;

		public void PredictDelta(uint tick, ref RhythmEngineSnapshotData baseline1, ref RhythmEngineSnapshotData baseline2)
		{
		}

		public void Serialize(ref RhythmEngineSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			var boolBitFields = default(byte);
			MainBit.SetBitAt(ref boolBitFields, 0, IsPaused);
			MainBit.SetBitAt(ref boolBitFields, 1, UseClientSimulation);
			MainBit.SetBitAt(ref boolBitFields, 2, ComboIsFever);

			writer.WritePackedUInt(boolBitFields, compressionModel);

			// 2
			writer.WritePackedUIntDelta(MaxBeats, baseline.MaxBeats, compressionModel);
			writer.WritePackedUIntDelta(BeatInterval, baseline.BeatInterval, compressionModel);

			// 7
			writer.WritePackedUIntDelta((uint) OwnerGhostId, (uint) baseline.OwnerGhostId, compressionModel);
			writer.WritePackedIntDelta(StartTime, baseline.StartTime, compressionModel);
			writer.WritePackedIntDelta(CommandTypeId, baseline.CommandTypeId, compressionModel);
			writer.WritePackedIntDelta(CommandStartTime, baseline.CommandStartTime, compressionModel);
			writer.WritePackedIntDelta(CommandEndTime, baseline.CommandEndTime, compressionModel);
			writer.WritePackedIntDelta(CommandChainEndTime, baseline.CommandChainEndTime, compressionModel);
			writer.WritePackedIntDelta(Recovery, baseline.Recovery, compressionModel);

			// 5
			writer.WritePackedIntDelta(ComboScore, baseline.ComboScore, compressionModel);
			writer.WritePackedUIntDelta(ComboChain, baseline.ComboChain, compressionModel);
			writer.WritePackedUIntDelta(ComboChainToFever, baseline.ComboChainToFever, compressionModel);
			writer.WritePackedUIntDelta(ComboJinnEnergy, baseline.ComboJinnEnergy, compressionModel);
			writer.WritePackedUIntDelta(ComboJinnEnergyMax, baseline.ComboJinnEnergyMax, compressionModel);
		}

		public void Deserialize(uint tick, ref RhythmEngineSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			var boolBitFields = (byte) reader.ReadPackedUInt(ref ctx, compressionModel);

			// 2
			MaxBeats     = reader.ReadPackedUIntDelta(ref ctx, baseline.MaxBeats, compressionModel);
			BeatInterval = reader.ReadPackedUIntDelta(ref ctx, baseline.BeatInterval, compressionModel);

			// 7
			OwnerGhostId        = (int) reader.ReadPackedUIntDelta(ref ctx, (uint) baseline.OwnerGhostId, compressionModel);
			StartTime           = reader.ReadPackedIntDelta(ref ctx, baseline.StartTime, compressionModel);
			CommandTypeId       = reader.ReadPackedIntDelta(ref ctx, baseline.CommandTypeId, compressionModel);
			CommandStartTime    = reader.ReadPackedIntDelta(ref ctx, baseline.CommandStartTime, compressionModel);
			CommandEndTime      = reader.ReadPackedIntDelta(ref ctx, baseline.CommandEndTime, compressionModel);
			CommandChainEndTime = reader.ReadPackedIntDelta(ref ctx, baseline.CommandChainEndTime, compressionModel);
			Recovery            = reader.ReadPackedIntDelta(ref ctx, baseline.Recovery, compressionModel);

			// 5
			ComboScore         = reader.ReadPackedIntDelta(ref ctx, baseline.ComboScore, compressionModel);
			ComboChain         = reader.ReadPackedUIntDelta(ref ctx, baseline.ComboChain, compressionModel);
			ComboChainToFever  = reader.ReadPackedUIntDelta(ref ctx, baseline.ComboChainToFever, compressionModel);
			ComboJinnEnergy    = reader.ReadPackedUIntDelta(ref ctx, baseline.ComboJinnEnergy, compressionModel);
			ComboJinnEnergyMax = reader.ReadPackedUIntDelta(ref ctx, baseline.ComboJinnEnergyMax, compressionModel);

			IsPaused            = MainBit.GetBitAt(boolBitFields, 0) == 1;
			UseClientSimulation = MainBit.GetBitAt(boolBitFields, 1) == 1;
			ComboIsFever        = MainBit.GetBitAt(boolBitFields, 2) == 1;
		}

		public void Interpolate(ref RhythmEngineSnapshotData target, float factor)
		{
			IsPaused            = target.IsPaused;
			UseClientSimulation = target.UseClientSimulation;

			MaxBeats     = target.MaxBeats;
			BeatInterval = target.BeatInterval;

			OwnerGhostId        = target.OwnerGhostId;
			StartTime           = target.StartTime;
			CommandTypeId       = target.CommandTypeId;
			CommandStartTime    = target.CommandStartTime;
			CommandEndTime      = target.CommandEndTime;
			CommandChainEndTime = target.CommandChainEndTime;
			Recovery            = target.Recovery;

			ComboIsFever       = target.ComboIsFever;
			ComboScore         = target.ComboScore;
			ComboChain         = target.ComboChain;
			ComboChainToFever  = target.ComboChainToFever;
			ComboJinnEnergy    = (uint) math.lerp(ComboJinnEnergy, target.ComboJinnEnergy, factor);
			ComboJinnEnergyMax = target.ComboJinnEnergyMax;
		}
	}
}