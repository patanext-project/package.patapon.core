using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GamePlay.RhythmEngine
{
	public struct GamePredictedCommandState : IComponentData
	{
		public GameCommandState State;
	}

	public struct GameCommandState : IReadWriteComponentSnapshot<GameCommandState>
	{
		public int StartTime;
		public int EndTime;
		public int ChainEndTime;

		public bool IsGamePlayActive(int milliseconds)
		{
			return milliseconds >= StartTime && milliseconds <= EndTime;
		}

		public bool IsInputActive(int milliseconds, int beatInterval)
		{
			return milliseconds >= EndTime - beatInterval && milliseconds <= EndTime + beatInterval;
		}

		public bool HasActivity(int milliseconds, int beatInterval)
		{
			return IsGamePlayActive(milliseconds)
			       || IsInputActive(milliseconds, beatInterval);
		}

		public void WriteTo(DataStreamWriter writer, ref GameCommandState baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			writer.WritePackedIntDelta(StartTime, baseline.StartTime, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(EndTime, baseline.EndTime, jobData.NetworkCompressionModel);
			writer.WritePackedIntDelta(ChainEndTime, baseline.ChainEndTime, jobData.NetworkCompressionModel);
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref GameCommandState baseline, DeserializeClientData jobData)
		{
			StartTime    = reader.ReadPackedIntDelta(ref ctx, baseline.StartTime, jobData.NetworkCompressionModel);
			EndTime      = reader.ReadPackedIntDelta(ref ctx, baseline.EndTime, jobData.NetworkCompressionModel);
			ChainEndTime = reader.ReadPackedIntDelta(ref ctx, baseline.ChainEndTime, jobData.NetworkCompressionModel);
		}
	}
}