using System.Net;
using EcsComponents.MasterServer;
using P4TLB.MasterServer;
using Patapon4TLB.Core;
using Patapon4TLB.Core.MasterServer;
using StormiumTeam.GameBase.Bootstraping;
using StormiumTeam.GameBase.External.Discord;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Bootstraps
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	public class DiscordToMasterServerBootstrap : BaseBootstrapSystem
	{
		public class IsActive : IComponentData
		{
		}

		private EntityQuery m_LocalDiscordUser;

		protected override void Register(Entity bootstrap)
		{
			EntityManager.SetComponentData(bootstrap, new BootstrapComponent {Name = nameof(DiscordToMasterServerBootstrap)});
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
				if (world.GetExistingSystem<DiscordToMasterServerClientTestSystem>() != null)
				{
					world.EntityManager.CreateEntity(typeof(IsActive));
				}
			}

			EntityManager.DestroyEntity(bootstrapSingleton);
		}
	}

	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class DiscordToMasterServerClientTestSystem : ComponentSystem
	{
		private P4ConnectToMasterServerFromDiscord m_ConnectionSystem;
		private EntityQuery                        m_ExistingOrPendingConnectionQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			RequireSingletonForUpdate<DiscordToMasterServerBootstrap.IsActive>();
			m_ConnectionSystem = World.GetOrCreateSystem<P4ConnectToMasterServerFromDiscord>();

			m_ExistingOrPendingConnectionQuery = GetEntityQuery(new EntityQueryDesc
			{
				Any = new ComponentType[] {typeof(RequestUserLogin), typeof(ConnectedMasterServerClient)}
			});
		}

		protected override void OnUpdate()
		{
			if (m_ConnectionSystem.IsCurrentlyRequesting || m_ExistingOrPendingConnectionQuery.CalculateEntityCount() != 0)
				return;

			m_ConnectionSystem.Request();
		}
	}
}