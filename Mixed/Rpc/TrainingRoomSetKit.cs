using Unity.Burst;
using Unity.Collections;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Rpc
{
	[BurstCompile]
	public struct TrainingRoomSetKit : IRpcCommand
	{
		public int KitId;

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(KitId);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			KitId = reader.ReadInt(ref ctx);
		}

		[BurstCompile]
		public static void OnExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<TrainingRoomSetKit>(ref parameters);
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(OnExecute);
		}

		public class RpcSystem : RpcCommandRequestSystem<TrainingRoomSetKit>
		{
		}
	}
}