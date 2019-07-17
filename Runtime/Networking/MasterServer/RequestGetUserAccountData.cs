using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Boo.Lang;
using Google.Protobuf;
using Grpc.Core;
using P4TLB.MasterServer;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEditor.UIElements;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public struct RequestGetUserAccountData : IMasterServerRequest, IComponentData
	{
		public struct Processing : IComponentData
		{}
		
		public int ErrorCode; // enum?
		
		public bool error => ErrorCode != 0;

		public long UserGuid;
	}

	public struct ResultGetUserAccountData : IComponentData
	{
		public NativeString64 Login;
	}

	public struct ConnectedMasterServerClient : IComponentData
	{
		/// NEVER SHARE IT
		public NativeString64 Token;
		public int ClientId;
	}

	public struct MasterServerIsPlayer : IComponentData
	{
	}

	public struct MasterServerIsServer : IComponentData
	{
	}

	[UpdateInGroup(typeof(MasterServerProcessRpcSystem))]
	[AlwaysUpdateSystem]
	public class MasterServerRequestUserAccountSystem : GameBaseSystem
	{
		private struct UserAccountClient : ISystemStateComponentData
		{
		}

		private MasterServerSystem m_MasterServer;
		private EntityQuery        m_MasterServerWithoutClient;

		public AuthenticationService.AuthenticationServiceClient Client;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_MasterServer = BootWorld.GetOrCreateSystem<MasterServerSystem>();
			m_MasterServerWithoutClient = GetEntityQuery(typeof(MasterServerConnection), ComponentType.Exclude<UserAccountClient>());
		}

		protected override void OnUpdate()
		{
			Entities.With(m_MasterServerWithoutClient).ForEach((Entity e, MasterServerConnection connection) =>
			{
				if (m_MasterServer.channel == null)
					return;
				
				Client = new AuthenticationService.AuthenticationServiceClient(m_MasterServer.channel);

				PostUpdateCommands.AddComponent(e, new UserAccountClient());
			});
			
			if (Client == null)
				return;
			
		}
	}
}