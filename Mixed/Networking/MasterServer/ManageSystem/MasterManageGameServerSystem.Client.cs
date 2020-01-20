using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EcsComponents.MasterServer;
using ENet;
using P4TLB.MasterServer;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core.MasterServer.Data;
using Patapon4TLB.Core.MasterServer.P4;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public class FoundServer : IComponentData
	{
		public int               Index;
		public ServerInformation Base;
		public string            Name  => Base.Name;
		public string            Login => Base.ServerUserLogin;
		public ulong             Id    => Base.ServerUserId;
	}

	public class MasterManageGameServerClientSystem : BaseSystemMasterServerService
	{
		private MsRequestModule<RequestServerList, RequestServerList.Processing, ResponseServiceList, RequestServerList.CompletionStatus>                    m_ServerListModule;
		private MsRequestModule<RequestConnectToServer, RequestConnectToServer.Processing, ResponseConnectToServer, RequestConnectToServer.CompletionStatus> m_ConnectToServerModule;
		private MsRequestModule<RequestDisconnectFromServer, RequestDisconnectFromServer.Processing, ResponseDisconnectFromServer, RequestDisconnectFromServer.CompletionStatus> m_DisconnectFromServerModule;

		private MsRequestModule<RequestServerInformation, RequestServerInformation.Processing, ResultServerInformation, RequestServerInformation.CompletionStatus> m_ServerInformationModule;
		private BaseMsAutoRequestModule                                                                                                                            m_AutomatedServerInformationModule;

		private NetworkStreamReceiveSystem m_NetworkStreamReceiveSystem;
		private EntityQuery                m_FoundServerQuery;
		private EntityQuery                m_CurrentServerQuery;

		private MasterServerManagePendingEventSystem m_PendingEventSystem;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_ServerListModule);
			GetModule(out m_ConnectToServerModule);
			GetModule(out m_ServerInformationModule);
			GetModule(out m_DisconnectFromServerModule);

			m_AutomatedServerInformationModule = MsAutomatedRequestModule.From(default(RequestServerInformation.Automated), m_ServerInformationModule);
			m_AutomatedServerInformationModule.SetPushComponents(typeof(MasterServerGlobalServerPush));

			m_FoundServerQuery   = GetEntityQuery(typeof(FoundServer));
			m_CurrentServerQuery = GetEntityQuery(typeof(CurrentServerSingleton));

			m_PendingEventSystem = World.GetOrCreateSystem<MasterServerManagePendingEventSystem>();
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

			m_DisconnectFromServerModule.Update();
			m_DisconnectFromServerModule.AddProcessTagToAllRequests();

			foreach (var kvp in m_DisconnectFromServerModule.GetRequests())
			{
				if (HasSingleton<NetworkStreamConnection>())
				{
					EntityManager.AddComponent(GetSingletonEntity<NetworkStreamConnection>(), typeof(NetworkStreamRequestDisconnect));
				}
				
				if (HasSingleton<CurrentServerSingleton>())
					EntityManager.DestroyEntity(GetSingletonEntity<CurrentServerSingleton>());

				m_DisconnectFromServerModule.InvokeDefaultOnResult(kvp.Entity, new RequestDisconnectFromServer.CompletionStatus {error = false}, out _);
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

					using (var entities = m_FoundServerQuery.ToEntityArray(Allocator.TempJob))
					{
						var i = 0;
						foreach (var server in result.Servers)
						{
							Entity existingTarget = default;
							for (var ent = 0; ent != entities.Length; ent++)
							{
								var oldData = EntityManager.GetComponentData<FoundServer>(entities[ent]);
								if (oldData.Base.ServerUserLogin == server.ServerUserLogin)
								{
									existingTarget = entities[ent];
									break;
								}
							}

							FoundServer newData = new FoundServer();
							newData.Index = i++;
							newData.Base  = server;

							if (existingTarget != default)
								EntityManager.SetComponentData(existingTarget, newData);
							else
							{
								EntityManager.AddComponentData(EntityManager.CreateEntity(typeof(FoundServer)), newData);
							}
						}
					}
				}
			}

			if (!HasSingleton<ConnectedMasterServerClient>())
				return;
			var connectedClient = GetSingleton<ConnectedMasterServerClient>();

			m_AutomatedServerInformationModule.Update();

			m_ConnectToServerModule.Update();
			m_ConnectToServerModule.AddProcessTagToAllRequests();

			if (m_PendingEventSystem.IsPending(nameof(GlobalEvents.OnClientServerUpdate)))
			{
				m_PendingEventSystem.DeleteEvent(nameof(GlobalEvents.OnClientServerUpdate));
				Entities.ForEach((Entity entity, ref RequestServerInformation.Automated automated) => { EntityManager.AddComponent(entity, typeof(MasterServerGlobalServerPush)); });
			}

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
						var addr = new Address();
						addr.SetHost(connectResponse.EndPointAddress);
						addr.Port = (ushort) connectResponse.EndPointPort;
						m_NetworkStreamReceiveSystem.Connect(addr);
					}

					if (m_CurrentServerQuery.IsEmptyIgnoreFilter)
					{
						EntityManager.CreateEntity(typeof(CurrentServerSingleton), typeof(RequestServerInformation.Automated));
					}

					var serverEntity = m_CurrentServerQuery.GetSingletonEntity();
					EntityManager.SetComponentData(serverEntity, new CurrentServerSingleton {ServerId             = request.ServerUserId});
					EntityManager.SetComponentData(serverEntity, new RequestServerInformation.Automated {ServerId = request.ServerUserId, ServerLogin = request.ServerLogin});
					EntityManager.AddComponent(serverEntity, typeof(MasterServerGlobalServerPush));
				}
			}

			m_ServerInformationModule.Update();
			m_ServerInformationModule.AddProcessTagToAllRequests();

			foreach (var kvp in m_ServerInformationModule.GetRequests())
			{
				var entity  = kvp.Entity;
				var request = kvp.Value;
				var response = await service.GetServerInformationAsync(new ServerInformationRequest
				{
					ClientToken     = connectedClient.Token.ToString(),
					ServerUserId    = request.ServerUserId,
					ServerUserLogin = request.ServerLogin.ToString()
				});
				if (m_ServerInformationModule.InvokeDefaultOnResult(entity, new RequestServerInformation.CompletionStatus {error = false}, out var responseEntity))
				{
					EntityManager.SetComponentData(responseEntity, new ResultServerInformation {Information = response.Information});
				}
			}
		}
	}
}