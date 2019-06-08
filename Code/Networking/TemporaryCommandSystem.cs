using Unity.NetCode;
using Unity.Networking.Transport;

namespace P4.Core.Code.Networking
{
	public struct TemporaryCommand : ICommandData<TemporaryCommand>
	{
		public uint Tick { get; set; }
		public void Serialize(DataStreamWriter writer)
		{
			
		}

		public void Deserialize(uint tick, DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			Tick = tick;
		}
	}
	
	public class TemporaryCommandSendSystem : CommandSendSystem<TemporaryCommand>
	{
		
	}

	public class TemporaryCommandReceiveSystem : CommandReceiveSystem<TemporaryCommand>
	{
		
	}
}