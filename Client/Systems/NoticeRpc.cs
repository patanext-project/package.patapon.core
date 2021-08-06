using GameHost.Core.RPC.Interfaces;

namespace PataNext.Client.Systems
{
	public struct NoticeRpc : IGameHostRpcPacket
	{
		public string MethodName => "PataNext.Tests.Notice";
		
		public bool   IsConnectedToServer;
	}
}