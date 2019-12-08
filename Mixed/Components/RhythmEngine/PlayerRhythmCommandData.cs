using Unity.NetCode;
using Unity.Networking.Transport;

namespace Components.RhythmEngine
{
	public struct PlayerRhythmCommandData : ICommandData<PlayerRhythmCommandData>
	{
		public uint Tick { get; set; }
		public void ReadFrom(DataStreamReader reader, ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
		{
			
		}

		public void WriteTo(DataStreamWriter writer, NetworkCompressionModel compressionModel)
		{
			
		}

		public void ReadFrom(DataStreamReader reader, ref DataStreamReader.Context ctx, PlayerRhythmCommandData baseline, NetworkCompressionModel compressionModel)
		{
			
		}

		public void WriteTo(DataStreamWriter writer, PlayerRhythmCommandData baseline, NetworkCompressionModel compressionModel)
		{
			
		}
	}
}