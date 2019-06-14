using DefaultNamespace;
using package.stormiumteam.shared;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon4TLB.Default.Snapshot
{
	public struct RhythmEngineSnapshotData : ISnapshotData<RhythmEngineSnapshotData>
	{
		public uint Tick { get; set; }

		public bool IsPaused;
		public bool UseClientSimulation;
		public int  MaxBeats;
		public int  BeatInterval;
		public int  Beat;

		public int OwnerGhostId;
		public int StartTime;     // in ms
		public int CommandTypeId; // CommandState.IsActive will be set if ghostId is null or not
		public int CommandStartBeat;
		public int CommandEndBeat;
		public int Recovery;

		public int  ComboScore;
		public int  ComboChain;
		public int  ComboChainToFever;
		public int  ComboJinnEnergy;
		public int  ComboJinnEnergyMax;
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

			writer.WritePackedUInt((uint) MaxBeats, compressionModel);
			writer.WritePackedUInt((uint) BeatInterval, compressionModel);
			writer.WritePackedUInt((uint) Beat, compressionModel);

			writer.WritePackedUInt((uint) OwnerGhostId, compressionModel);
			writer.WritePackedUInt((uint) StartTime, compressionModel);
			writer.WritePackedUInt((uint) CommandTypeId, compressionModel);
			writer.WritePackedUInt((uint) CommandStartBeat, compressionModel);
			writer.WritePackedUInt((uint) CommandEndBeat, compressionModel);
			writer.WritePackedUInt((uint) Recovery, compressionModel);

			writer.WritePackedInt(ComboScore, compressionModel);
			writer.WritePackedInt(ComboChain, compressionModel);
			writer.WritePackedInt(ComboChainToFever, compressionModel);
			writer.WritePackedInt(ComboJinnEnergy, compressionModel);
			writer.WritePackedInt(ComboJinnEnergyMax, compressionModel);
		}

		public void Deserialize(uint tick, ref RhythmEngineSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			var boolBitFields = (byte) reader.ReadPackedUInt(ref ctx, compressionModel);

			MaxBeats     = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			BeatInterval = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			Beat         = (int) reader.ReadPackedUInt(ref ctx, compressionModel);

			OwnerGhostId     = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			StartTime        = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			CommandTypeId    = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			CommandStartBeat = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			CommandEndBeat   = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			Recovery         = (int) reader.ReadPackedUInt(ref ctx, compressionModel);

			ComboScore         = reader.ReadPackedInt(ref ctx, compressionModel);
			ComboChainToFever  = reader.ReadPackedInt(ref ctx, compressionModel);
			ComboJinnEnergy    = reader.ReadPackedInt(ref ctx, compressionModel);
			ComboJinnEnergyMax = reader.ReadPackedInt(ref ctx, compressionModel);

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
			Beat         = target.Beat;

			OwnerGhostId     = target.OwnerGhostId;
			StartTime        = target.StartTime;
			CommandTypeId    = target.CommandTypeId;
			CommandStartBeat = target.CommandStartBeat;
			CommandEndBeat   = target.CommandEndBeat;

			ComboIsFever       = target.ComboIsFever;
			ComboScore         = target.ComboScore;
			ComboChain         = target.ComboChain;
			ComboChainToFever  = target.ComboChainToFever;
			ComboJinnEnergy    = (int) math.lerp(ComboJinnEnergy, target.ComboJinnEnergy, factor);
			ComboJinnEnergyMax = target.ComboJinnEnergyMax;
		}
	}
}