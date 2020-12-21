using GameHost.Core;
using GameHost.Core.RPC;
using RevolutionSnapshot.Core.Buffers;
using Unity.Collections;
using UnityEngine.Events;

namespace PataNext.Client.Systems
{
	public class NoticeRpc : RpcCommandSystem
	{
		public struct CurrentNotice
		{
			public bool IsConnectedToServer;
		}

		public override string CommandId => "notice";

		protected override void OnReceiveRequest(GameHostCommandResponse response)
		{

		}

		public CurrentNotice Notice { get; private set; }
		public UnityEvent    OnNoticeReceived = new UnityEvent();

		protected override void OnReceiveReply(GameHostCommandResponse response)
		{
			Notice = new CurrentNotice
			{
				IsConnectedToServer = response.Data.ReadValue<bool>()
			};

			OnNoticeReceived.Invoke();
		}

		public void Send()
		{
			var connector = World.GetExistingSystem<GameHostConnector>();
			var buffer    = new DataBufferWriter(1, Allocator.Persistent);

			connector.BroadcastRequest(CommandId, buffer);

			buffer.Dispose();
		}
	}
}