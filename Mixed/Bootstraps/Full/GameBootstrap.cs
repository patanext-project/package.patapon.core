using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EcsComponents.MasterServer;
using ENet;
using Newtonsoft.Json;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.VSHeadOn;
using Patapon4TLB.Core.MasterServer;
using Patapon4TLB.Core.MasterServer.Data;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Bootstraping;
using StormiumTeam.GameBase.External.Discord;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Bootstraps.Full
{
	public struct MasterServerRule : IComponentData
	{
		public NativeString64 Address;
		public int            Port;
	}

	public class P4MasterServerRule : RuleBaseSystem<MasterServerRule>
	{
		public RuleProperties<MasterServerRule>.Property<NativeString64> Address;
		public RuleProperties<MasterServerRule>.Property<int>            Port;

		protected override void AddRuleProperties()
		{
			Address = Rule.Add(d => d.Address);
			Port    = Rule.Add(d => d.Port);
		}

		protected override void SetDefaultProperties()
		{
			Address.Value = "82.64.86.228";
			Port.Value    = 4242;
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class GameBootstrap : BaseBootstrapSystem
	{
		public struct IsActive : IComponentData
		{
		}
		
		public struct LaunchVsServer : IComponentData {}

		private Task m_ConnectTask;

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(GameBootstrap)});

			var masterServer = World.GetOrCreateSystem<MasterServerSystem>();

			// Set the target of our MasterServer here
			var msRule = World.GetOrCreateSystem<P4MasterServerRule>();
			m_ConnectTask = masterServer.SetMasterServer(new IPEndPoint(IPAddress.Parse(msRule.Address.Value.ToString()), msRule.Port.Value));

			Application.targetFrameRate = 150;
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			foreach (var world in World.AllWorlds)
			{
				world.EntityManager.SetComponentData(world.EntityManager.CreateEntity(typeof(GameProtocolVersion)), new GameProtocolVersion {Version = GameStatic.Version});
				world.EntityManager.CreateEntity(typeof(IsActive));

				if (EntityManager.GetComponentData<BootstrapParameters>(bootstrapSingleton)
				                 .Values
				                 .Contains("vs"))
					world.EntityManager.CreateEntity(typeof(LaunchVsServer));
			}

			EntityManager.DestroyEntity(bootstrapSingleton);
		}

#if UNITY_EDITOR
		[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
		public class ServerSystem : GameBaseSystem
		{
			private bool m_SetCustomServerName;

			public string Name;

			private EntityQuery m_PlayerQuery;
			private int         m_PreviousUserCount;

			protected override void OnCreate()
			{
				base.OnCreate();
				RequireSingletonForUpdate<IsActive>();
				RequireSingletonForUpdate<LaunchVsServer>();

				Name = "P<color=#ED1C24>4</color> Test Server, yes yes yes";
				//Name = "New Version in WIP, don't join yet!";

				m_PlayerQuery = GetEntityQuery(typeof(GamePlayer), typeof(GamePlayerReadyTag));
			}

			protected override void OnStartRunning()
			{
				base.OnStartRunning();

				var ep = NetworkEndPoint.AnyIpv4;
				ep.Port = 5605;
				
				var addr = new Address();
				addr.Port = 5605;

				World.GetExistingSystem<NetworkStreamReceiveSystem>()
				     .Listen(addr);

				var networkStreamReceive = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
				var ent                  = EntityManager.CreateEntity(typeof(RequestUserLogin));
				EntityManager.SetComponentData(ent, new RequestUserLogin
				{
					Login = new NativeString64("server_0"),
					RoutedData = new NativeString512(JsonConvert.SerializeObject(new
					{
						addr = "127.0.0.1",
						port = 5605
					}))
					// local server does not need password for now
				});

				EntityManager.CreateEntity(typeof(GameModePreMatch));
				World.GetOrCreateSystem<GameModeManager>().SetGameMode(new MpVersusHeadOn());
			}

			protected override void OnUpdate()
			{
				if (!HasSingleton<ConnectedMasterServerClient>())
					return;

				if (!m_SetCustomServerName)
				{
					m_SetCustomServerName = true;
					var request = EntityManager.CreateEntity(typeof(RequestUpdateServerInformation));
					EntityManager.SetComponentData(request, new RequestUpdateServerInformation
					{
						Name     = Name,
						MaxUsers = 8
					});
				}

				if (m_SetCustomServerName && m_PreviousUserCount != m_PlayerQuery.CalculateEntityCount())
				{
					m_PreviousUserCount = m_PlayerQuery.CalculateEntityCount();
					var request = EntityManager.CreateEntity(typeof(RequestUpdateServerInformation));
					EntityManager.SetComponentData(request, new RequestUpdateServerInformation
					{
						Name             = Name,
						CurrentUserCount = m_PreviousUserCount,
						MaxUsers         = 8
					});
				}
			}
		}
#endif
	}
}