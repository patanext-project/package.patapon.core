using System.Net;
using EcsComponents.MasterServer;
using Newtonsoft.Json;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer;
using Patapon4TLB.Core.MasterServer.Data;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Bootstraping;
using StormiumTeam.GameBase.External.Discord;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Bootstraps
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class ClientServerMasterServerBootstrap : BaseBootstrapSystem
	{
		public class IsActive : IComponentData
		{
		}

		private EntityQuery m_LocalDiscordUser;

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(ClientServerMasterServerBootstrap)});
			m_LocalDiscordUser = GetEntityQuery(typeof(DiscordLocalUser));

			var masterServer = World.GetOrCreateSystem<MasterServerSystem>();

			// Set the target of our MasterServer here
			masterServer.SetMasterServer(new IPEndPoint(IPAddress.Parse("82.64.86.228"), 4242));
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			if (m_LocalDiscordUser.CalculateEntityCount() == 0)
				return;

			foreach (var world in World.AllWorlds)
			{
				/*if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
				{
					world.EntityManager.CreateEntity(typeof(IsActive));
				}
				else if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
				{
					world.EntityManager.CreateEntity(typeof(IsActive));

					// Server world automatically listen for connections from any host
					var ep = NetworkEndPoint.AnyIpv4;
					ep.Port = 7979;
					world.GetOrCreateSystem<NetworkStreamReceiveSystem>().Listen(ep);
				}*/
			}

			EntityManager.DestroyEntity(bootstrapSingleton);
		}

		[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
		public class ClientSystem : GameBaseSystem
		{
			private EntityQuery m_AnyConnectionOrPendingQuery;
			private Entity      m_ServerListRequest;
			private ulong       m_ServerConnectionTarget;
			private bool        m_HasSentRequest;

			protected override void OnCreate()
			{
				base.OnCreate();
				RequireSingletonForUpdate<IsActive>();

				m_AnyConnectionOrPendingQuery = GetEntityQuery(new EntityQueryDesc
				{
					Any = new ComponentType[] {typeof(RequestUserLogin), typeof(ConnectedMasterServerClient)}
				});
			}

			protected override void OnUpdate()
			{
				if (!World.GetExistingSystem<P4ConnectToMasterServerFromDiscord>().IsCurrentlyRequesting && m_AnyConnectionOrPendingQuery.IsEmptyIgnoreFilter)
				{
					World.GetExistingSystem<P4ConnectToMasterServerFromDiscord>().Request();
				}

				if (!HasSingleton<ConnectedMasterServerClient>())
					return;

				// phase 1: search for an active server
				if (m_ServerConnectionTarget == 0)
				{
					if (m_ServerListRequest == Entity.Null)
					{
						m_ServerListRequest = EntityManager.CreateEntity(typeof(RequestServerList));
						EntityManager.SetComponentData(m_ServerListRequest, new RequestServerList {Query = new NativeString512()});
					}
					else
					{
						if (EntityManager.HasComponent<ResponseServiceList>(m_ServerListRequest))
						{
							var response = EntityManager.GetComponentData<ResponseServiceList>(m_ServerListRequest);
							foreach (var server in response.Servers)
							{
								Debug.Log($"Name: {server.Name}, Id: {server.ServerUserId}, Login: {server.ServerUserLogin}");
								m_ServerConnectionTarget = server.ServerUserId;
							}
						}

						if (EntityManager.HasComponent<RequestServerList.CompletionStatus>(m_ServerListRequest))
						{
							EntityManager.DestroyEntity(m_ServerListRequest);
							m_ServerListRequest = Entity.Null;
						}
					}
				}

				// phase 2: connect to the server 
				if (m_ServerConnectionTarget != 0 && !m_HasSentRequest)
				{
					m_HasSentRequest = true;
					var req = EntityManager.CreateEntity(typeof(RequestConnectToServer));
					EntityManager.SetComponentData(req, new RequestConnectToServer {ServerUserId = m_ServerConnectionTarget});
				}
			}
		}

		[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
		public class ServerSystem : GameBaseSystem
		{
			protected override void OnCreate()
			{
				base.OnCreate();
				RequireSingletonForUpdate<IsActive>();
			}

			protected override void OnStartRunning()
			{
				base.OnStartRunning();
				var networkStreamReceive = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
				var ent                  = EntityManager.CreateEntity(typeof(RequestUserLogin));
				EntityManager.SetComponentData(ent, new RequestUserLogin
				{
					Login = new NativeString64("server_0"),
					RoutedData = new NativeString512(JsonConvert.SerializeObject(new
					{
						addr = "127.0.0.1",
						port = networkStreamReceive.Driver.LocalEndPoint().Port
					}))
					// local server does not need password for now
				});
			}

			protected override void OnUpdate()
			{

			}
		}
	}
}