using GameHost.Core;
using GameHost.Core.RPC;
using GameHost.Core.RPC.BaseSystems;
using GameHost.Core.RPC.Interfaces;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using UnityEngine.Events;

namespace PataNext.Client.Systems
{
	public struct NoticeRpc : IGameHostRpcPacket
	{
		public string MethodName => "PataNext.Tests.Notice";
		
		public bool   IsConnectedToServer;
	}
}