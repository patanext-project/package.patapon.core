using System.Collections.Generic;
using System.Threading.Tasks;
using EcsComponents.MasterServer;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core.MasterServer.Data;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public class MasterManageGameServerServerSystem : BaseSystemMasterServerService
	{
		private MsRequestModule<RequestUpdateServerInformation, RequestUpdateServerInformation.Processing, ResultUpdateServerInformation, RequestUpdateServerInformation.CompletionStatus> m_UpdateServerInformationModule;
		
		private MasterServerManagePendingEventSystem m_EventSystem;
		private NativeHashMap<ulong, NativeString64> m_UserTokenMap;

		protected override void OnCreate()
		{
			base.OnCreate();
			m_EventSystem  = World.GetOrCreateSystem<MasterServerManagePendingEventSystem>();
			
			GetModule(out m_UpdateServerInformationModule);
		}

		protected override async void OnUpdate()
		{
			if (World.GetExistingSystem<ServerSimulationSystemGroup>() == null)
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

				// await thing, blablablabla
#pragma warning disable 4014
				service.GetPendingConnectionTokensAsync(new GetPendingConnectionTokenRequest
				{
					ClientToken = connectedClient.Token.ToString()
				}).ResponseAsync.ContinueWith((ContinuationAction));
#pragma warning restore 4014
			}

			m_UpdateServerInformationModule.Update();
			m_UpdateServerInformationModule.AddProcessTagToAllRequests();

			foreach (var kvp in m_UpdateServerInformationModule.GetRequests())
			{
				var entity  = kvp.Entity;
				var request = kvp.Value;
				var result = await service.UpdateServerInformationAsync(new SetServerInformationRequest
				{
					ClientToken = connectedClient.Token.ToString(),
					Name        = request.Name.ToString(),
					SlotCount   = request.CurrentUserCount,
					SlotLimit   = request.MaxUsers
				});
				Debug.Log(result.Error);
				if (m_UpdateServerInformationModule.InvokeDefaultOnResult(entity, new RequestUpdateServerInformation.CompletionStatus {ErrorCode = result.Error}, out var responseEntity))
				{

				}
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