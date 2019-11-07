using Revolution;
using Unity.Networking.Transport;

namespace Patapon.Mixed.RhythmEngine
{
	public struct RhythmEngineState : IReadWriteComponentSnapshot<RhythmEngineState>
	{
		public bool IsPaused;
		public bool IsNewBeat;
		public bool IsNewPressure;

		/// <summary>
		/// If a user do a f**k-up (doing pressure in an active command, waited a beat too much,...), he will need to wait a beat before starting to do pressures.
		/// </summary>
		public int NextBeatRecovery;

		public bool ApplyCommandNextBeat;
		public bool VerifyCommand;
		public int LastPressureBeat;

		public bool IsRecovery(int processBeat)
		{
			return NextBeatRecovery > processBeat;
		}

		public void WriteTo(DataStreamWriter writer, ref RhythmEngineState baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedUIntDelta(IsPaused ? 1u : 0u, baseline.IsPaused ? 1u : 0u, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(NextBeatRecovery, baseline.NextBeatRecovery, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RhythmEngineState baseline, DeserializeClientData jobData)
		{
			IsPaused = reader.ReadPackedUIntDelta(ref ctx, baseline.IsPaused ? 1u : 0u, jobData.NetworkCompressionModel) == 1;
			NextBeatRecovery = reader.ReadPackedIntDelta(ref ctx, baseline.NextBeatRecovery, jobData.NetworkCompressionModel);
		}
	}
}