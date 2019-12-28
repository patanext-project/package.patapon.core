using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.RhythmEngine
{
	public struct RhythmEngineSettings : IReadWriteComponentSnapshot<RhythmEngineSettings>, ISnapshotDelta<RhythmEngineSettings>
	{
		public int MaxBeats;
		public int BeatInterval; // in ms

		/// <summary>
		/// Let the players simulate the rhythm engine or not.
		/// If enabled, 'RhythmEngineClientPredictedCommand' and 'RhythmEngineClientRequestedCommand' will be
		/// used instead of 'RhythmEngineCurrentCommand'.
		/// </summary>
		public bool UseClientSimulation;

		public void WriteTo(DataStreamWriter writer, ref RhythmEngineSettings baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedInt(MaxBeats, jobData.NetworkCompressionModel);
			writer.WritePackedInt(BeatInterval, jobData.NetworkCompressionModel);
			writer.WriteBitBool(UseClientSimulation);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RhythmEngineSettings baseline, DeserializeClientData jobData)
		{
			this = baseline;
			MaxBeats     = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);
			BeatInterval = reader.ReadPackedInt(ref ctx, jobData.NetworkCompressionModel);

			UseClientSimulation = reader.ReadBitBool(ref ctx);
		}
		
		public bool DidChange(RhythmEngineSettings baseline)
		{
			return MaxBeats != baseline.MaxBeats
			       || BeatInterval != baseline.BeatInterval
			       || UseClientSimulation != baseline.UseClientSimulation;
		}
		
		public struct Exclude : IComponentData
		{}

		public class Sync : MixedComponentSnapshotSystemDelta<RhythmEngineSettings>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
	}
}