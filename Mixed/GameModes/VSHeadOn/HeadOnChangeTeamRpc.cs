using Unity.Burst;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.GameModes.VSHeadOn
{
	[BurstCompile]
	public struct HeadOnChangeTeamRpc : IRpcCommand
	{
		public int Team;

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(Team);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			Team = reader.ReadInt(ref ctx);
		}

		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<HeadOnChangeTeamRpc>(ref parameters);
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
		}

		public class RpcSystem : RpcCommandRequestSystem<HeadOnChangeTeamRpc>
		{
		}
	}
}