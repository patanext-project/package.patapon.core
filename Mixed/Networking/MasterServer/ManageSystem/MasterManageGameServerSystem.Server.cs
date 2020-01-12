using System.Collections.Generic;
using System.Threading.Tasks;
using EcsComponents.MasterServer;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core.MasterServer.Data;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public class MasterManageGameServerServerSystem : BaseSystemMasterServerService
	{
		private MasterServerManagePendingEventSystem m_EventSystem;
		private NativeHashMap<ulong, NativeString64> m_UserTokenMap;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_EventSystem  = World.GetOrCreateSystem<MasterServerManagePendingEventSystem>();
		}

		protected override void OnUpdate()
		{
			if (!IsServer)
				return;
			else if (!m_UserTokenMap.IsCreated)
			{
				m_UserTokenMap = World.GetOrCreateSystem<CreateGamePlayerSystem>().TokenMap;
			}
				
			if (!StaticMasterServer.TryGetClient(out GameServerService.GameServerServiceClient service))
				return;

			if (!HasSingleton<ConnectedMasterServerClient>())
				return;

			var connectedClient = GetSingleton<ConnectedMasterServerClient>();
			if (m_EventSystem.IsPending(nameof(GlobalEvents.OnNewConnectionTokens)))
			{
				m_EventSystem.DeleteEvent(nameof(GlobalEvents.OnNewConnectionTokens));

				service.GetPendingConnectionTokensAsync(new GetPendingConnectionTokenRequest
				{
					ClientToken = connectedClient.Token.ToString()
				}).ResponseAsync.ContinueWith((ContinuationAction));
			}
		}

		private async void ContinuationAction(Task<GetPendingConnectionTokenResponse> task)
		{
			if (task.Result == null)
			{
				Debug.LogError("Couldn't get pending connection tokens!");
				return;
			}

			var service         = StaticMasterServer.GetClient<GameServerService.GameServerServiceClient>();
			var connectedClient = GetSingleton<ConnectedMasterServerClient>();

			var response = task.Result;
			foreach (var cc in response.List)
			{
				m_UserTokenMap[cc.UserId] = cc.Token;
				await service.AcknowledgeTokenAsync(new AcknowledgeTokenRequest
				{
					ClientToken        = connectedClient.Token.ToString(),
					AcknowledgedUserId = cc.UserId
				});
			}

			Debug.Log("token ack: " + response.List.Count);
		}
	}
}