using System.Net;
using package.stormiumteam.shared.ecs;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer;
using StormiumTeam.GameBase.Bootstraping;
using StormiumTeam.GameBase.External.Discord;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Bootstraps
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class SelectKitMasterServerBootstrap : BaseBootstrapSystem
	{
		public class IsActive : IComponentData
		{
		}

		private EntityQuery m_LocalDiscordUser;

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(SelectKitMasterServerBootstrap)});
			m_LocalDiscordUser = GetEntityQuery(typeof(DiscordLocalUser));

			var masterServer = World.GetOrCreateSystem<MasterServerSystem>();

			// Set the target of our MasterServer here
			masterServer.SetMasterServer(new IPEndPoint(IPAddress.Loopback, 4242));
		}

		protected override void Match(Entity bootstrapSingleton)
		{
			if (m_LocalDiscordUser.CalculateEntityCount() == 0)
				return;

			foreach (var world in World.AllWorlds)
			{
				if (world.GetExistingSystem<SelectKitMasterServerSystem>() != null)
				{
					world.EntityManager.CreateEntity(typeof(IsActive));
				}
			}

			EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class SelectKitMasterServerSystem : ComponentSystem
	{
		private EntityQuery m_AnyConnectionOrPendingQuery;
		private Entity      m_FormationRequest;

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireSingletonForUpdate<SelectKitMasterServerBootstrap.IsActive>();

			m_AnyConnectionOrPendingQuery = GetEntityQuery(new EntityQueryDesc
			{
				Any = new ComponentType[] {typeof(ConnectedMasterServerClient), typeof(RequestUserLogin)}
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

			var connectedClient = GetSingleton<ConnectedMasterServerClient>();
			if (m_FormationRequest == default)
			{
				Debug.Log("Searching for formation...");
				m_FormationRequest = EntityManager.CreateEntity(typeof(RequestGetUserFormationData));
				EntityManager.SetComponentData(m_FormationRequest, new RequestGetUserFormationData {UserId = connectedClient.UserId});
			}
			else
			{
				EntityManager.TryGetComponentData(m_FormationRequest, out RequestGetUserFormationData request);
				if (request.error)
				{
					Debug.LogError(request.ErrorCode);
					EntityManager.DestroyEntity(m_FormationRequest);
				}
				else if (EntityManager.HasComponent<ResultGetUserFormationData>(m_FormationRequest))
				{
					var result = EntityManager.GetComponentData<ResultGetUserFormationData>(m_FormationRequest);
					Debug.Log("Formation name: " + result.Root.Name);

					EntityManager.DestroyEntity(m_FormationRequest);
				}
			}
		}
	}
}