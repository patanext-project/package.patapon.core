using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.RhythmEngine.Rpc
{
	public struct PressureEventFromClientRpc : IRpcCommand
	{
		public int  Key;
		public int  FlowBeat;
		public bool ShouldStartRecovery;

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(Key);
			writer.Write(FlowBeat);
			writer.Write((byte) (ShouldStartRecovery ? 1 : 0));
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			Key                 = reader.ReadInt(ref ctx);
			FlowBeat            = reader.ReadInt(ref ctx);
			ShouldStartRecovery = reader.ReadByte(ref ctx) == 1;
		}

		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<PressureEventFromClientRpc>(ref parameters);
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
		}
	}
}