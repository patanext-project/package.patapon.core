using DefaultNamespace;
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
		public int StartTime; // in ms

		public void PredictDelta(uint tick, ref RhythmEngineSnapshotData baseline1, ref RhythmEngineSnapshotData baseline2)
		{
		}

		public void Serialize(ref RhythmEngineSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedUInt((uint) (IsPaused ? 1 : 0), compressionModel);
			writer.WritePackedUInt((uint) (UseClientSimulation ? 1 : 0), compressionModel);
			writer.WritePackedUInt((uint) MaxBeats, compressionModel);
			writer.WritePackedUInt((uint) BeatInterval, compressionModel);
			writer.WritePackedUInt((uint) Beat, compressionModel);
			writer.WritePackedUInt((uint) StartTime, compressionModel);
			writer.WritePackedUInt((uint) OwnerGhostId, compressionModel);
		}

		public void Deserialize(uint tick, ref RhythmEngineSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			IsPaused            = reader.ReadPackedUInt(ref ctx, compressionModel) == 1;
			UseClientSimulation = reader.ReadPackedUInt(ref ctx, compressionModel) == 1;
			MaxBeats            = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			BeatInterval        = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			Beat                = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			StartTime           = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			OwnerGhostId        = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
		}

		public void Interpolate(ref RhythmEngineSnapshotData target, float factor)
		{
			IsPaused            = target.IsPaused;
			UseClientSimulation = target.UseClientSimulation;
			MaxBeats            = target.MaxBeats;
			BeatInterval        = target.BeatInterval;
			Beat                = target.Beat;
			OwnerGhostId        = target.OwnerGhostId;
			StartTime           = target.StartTime;
		}
	}
}