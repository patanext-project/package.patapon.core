using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcsComponents.MasterServer;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core.MasterServer.Data;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public class MasterManageGameServerClientSystem : BaseSystemMasterServerService
	{
		private MsRequestModule<RequestServerList, RequestServerList.Processing, ResponseServiceList, RequestServerList.CompletionStatus>                    m_ServerListModule;
		private MsRequestModule<RequestConnectToServer, RequestConnectToServer.Processing, ResponseConnectToServer, RequestConnectToServer.CompletionStatus> m_ConnectToServerModule;

		private NetworkStreamReceiveSystem m_NetworkStreamReceiveSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_ServerListModule);
			GetModule(out m_ConnectToServerModule);
		}

		protected override async void OnUpdate()
		{
			if (World.GetExistingSystem<ClientSimulationSystemGroup>() == null)
				return;
			else
			{
				if (m_NetworkStreamReceiveSystem == null)
					m_NetworkStreamReceiveSystem = World.GetExistingSystem<NetworkStreamReceiveSystem>();
			}

			if (!StaticMasterServer.TryGetClient(out GameServerService.GameServerServiceClient service))
				return;

			m_ServerListModule.Update();
			m_ServerListModule.AddProcessTagToAllRequests();

			foreach (var kvp in m_ServerListModule.GetRequests())
			{
				var entity  = kvp.Entity;
				var request = kvp.Value;
				var result = await service.GetServerListAsync(new ServerListRequest
				{
					QueryString = request.Query.ToString()
				});
				if (m_ServerListModule.InvokeDefaultOnResult(entity, new RequestServerList.CompletionStatus {error = false}, out var responseEntity))
				{
					var serverList = EntityManager.GetComponentData<ResponseServiceList>(responseEntity);
					// The module can't know if the response is class or a struct, so we need to manage it like that...
					if (serverList == null)
					{
						serverList = new ResponseServiceList();
					}
					else
						serverList.Servers.Clear();

					serverList.Servers = new List<ServerInformation>();
					serverList.Servers.AddRange(result.Servers);

					EntityManager.SetComponentData(responseEntity, serverList);
				}
			}

			if (!HasSingleton<ConnectedMasterServerClient>())
				return;
			var connectedClient = GetSingleton<ConnectedMasterServerClient>();

			m_ConnectToServerModule.Update();
			m_ConnectToServerModule.AddProcessTagToAllRequests();

			foreach (var kvp in m_ConnectToServerModule.GetRequests())
			{
				var entity  = kvp.Entity;
				var request = kvp.Value;
				var connectionTokenResponse = await service.GetConnectionTokenAsync(new ConnectionTokenRequest
				{
					ClientToken     = connectedClient.Token.ToString(),
					ServerUserId    = request.ServerUserId,
					ServerUserLogin = request.ServerLogin.ToString()
				});
				if (string.IsNullOrEmpty(connectionTokenResponse.ConnectToken))
				{
					throw new InvalidOperationException("No connection token :(");
				}

				Task<ConnectionResponse> connectResponseTask = null;
				var                      tryCount            = 0;
				while (tryCount < 6)
				{
					connectResponseTask = service.TryConnectAsync(new ConnectionRequest
					{
						ClientToken     = connectedClient.Token.ToString(),
						ServerUserId    = request.ServerUserId,
						ServerUserLogin = request.ServerLogin.ToString()
					}).ResponseAsync;
					await connectResponseTask;
					if (connectResponseTask.Result.Error == ConnectionResponse.Types.ErrorCode.ServerAckPending)
					{
						await Task.Delay((tryCount + 1) * 250);
						tryCount += 1;
					}
					else
						break;
				}

				if (connectResponseTask == null)
					throw new NullReferenceException();

				var connectResponse = connectResponseTask.Result;
				if (m_ConnectToServerModule.InvokeDefaultOnResult
					(
						entity,
						new RequestConnectToServer.CompletionStatus {ConnectionErrorCode = connectResponse.Error},
						out var responseEntity
					)
				)
				{
					World.GetExistingSystem<ClientLoadSystem>()
					     .ConnectionToken = connectionTokenResponse.ConnectToken;

					Debug.Log($"server addr={connectResponse.EndPointAddress}:{connectResponse.EndPointPort}");
					if (!request.ManualConnectionLabor)
					{
						var nep = NetworkEndPoint.Parse(connectResponse.EndPointAddress, (ushort) connectResponse.EndPointPort);
						m_NetworkStreamReceiveSystem.Connect(nep);
					}
				}
			}
		}
	}
}