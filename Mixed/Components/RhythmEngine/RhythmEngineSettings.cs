using Revolution;
using Unity.Networking.Transport;

namespace Patapon.Mixed.RhythmEngine
{
	public struct RhythmEngineSettings : IReadWriteComponentSnapshot<RhythmEngineSettings>
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
			writer.WritePackedIntDelta(MaxBeats, baseline.MaxBeats, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(BeatInterval, baseline.BeatInterval, jobData.NetworkCompressionModel);
			writer.WritePackedUIntDelta(UseClientSimulation ? 1u : 0u, baseline.UseClientSimulation ? 1u : 0u, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RhythmEngineSettings baseline, DeserializeClientData jobData)
		{
			MaxBeats     = reader.ReadPackedIntDelta(ref ctx, baseline.MaxBeats, jobData.NetworkCompressionModel);
			BeatInterval = reader.ReadPackedIntDelta(ref ctx, baseline.BeatInterval, jobData.NetworkCompressionModel);

			UseClientSimulation = reader.ReadPackedUIntDelta(ref ctx, baseline.UseClientSimulation ? 1u : 0u, jobData.NetworkCompressionModel) == 1;
		}
	}
}