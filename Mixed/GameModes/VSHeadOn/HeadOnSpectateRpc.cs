using Unity.Burst;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	[BurstCompile]
	public struct HeadOnSpectateRpc : IRpcCommand
	{
		public uint GhostId;
		
		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(GhostId);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			GhostId = reader.ReadUInt(ref ctx);
		}

		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<HeadOnSpectateRpc>(ref parameters);
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
		}

		public class RpcSystem : RpcCommandRequestSystem<HeadOnSpectateRpc>
		{
		}
	}
}