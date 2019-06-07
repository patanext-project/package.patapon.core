using DefaultNamespace;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default.Snapshot
{
	public struct DefaultRhythmEngineSnapshotData : ISnapshotData<DefaultRhythmEngineSnapshotData>
	{
		public uint Tick { get; set; }

		public bool IsPaused;
		public int  MaxBeats;
		public int  Beat;

		public void PredictDelta(uint tick, ref DefaultRhythmEngineSnapshotData baseline1, ref DefaultRhythmEngineSnapshotData baseline2)
		{
		}

		public void Serialize(ref DefaultRhythmEngineSnapshotData baseline, DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			writer.WritePackedUInt((uint) (IsPaused ? 1 : 0), compressionModel);
			writer.WritePackedUInt((uint) MaxBeats, compressionModel);
			writer.WritePackedUInt((uint) Beat, compressionModel);
		}

		public void Deserialize(uint tick, ref DefaultRhythmEngineSnapshotData baseline, DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			Tick = tick;

			IsPaused = reader.ReadPackedUInt(ref ctx, compressionModel) == 1;
			MaxBeats = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
			Beat     = (int) reader.ReadPackedUInt(ref ctx, compressionModel);
		}

		public void Interpolate(ref DefaultRhythmEngineSnapshotData target, float factor)
		{
			IsPaused = target.IsPaused;
			MaxBeats = target.MaxBeats;
			Beat     = target.Beat;
		}
	}
}