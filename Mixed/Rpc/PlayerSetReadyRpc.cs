using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Rpc
{
	[BurstCompile]
	public struct PlayerSetReadyRpc : IRpcCommand
	{
		public bool Value;

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write((byte) (Value ? 1 : 0));
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			Value = reader.ReadByte(ref ctx) == 1;
		}

		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<PlayerSetReadyRpc>(ref parameters);
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
		}

		public class RpcSystem : RpcCommandRequestSystem<PlayerSetReadyRpc>
		{
		}
	}
}