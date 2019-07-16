using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Boo.Lang;
using Google.Protobuf;
using Grpc.Core;
using P4TLB.MasterServer;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEditor.UIElements;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public struct RequestUserLogin : IMasterServerRequest, IComponentData
	{
		public struct Processing : IComponentData
		{}
		
		public UserLoginResponse.Types.ErrorCode ErrorCode;
		
		public bool error => ErrorCode != UserLoginResponse.Types.ErrorCode.Success;

		public NativeString64 Login;
		public NativeString512 HashedPassword;
		public UserLoginRequest.Types.RequestType Type;

		public RequestUserLogin(string login, string password, UserLoginRequest.Types.RequestType type)
		{
			Login = new NativeString64(login);
			HashedPassword = new NativeString512(password);
			Type = type;

			ErrorCode = UserLoginResponse.Types.ErrorCode.Invalid;
		}
	}

	public struct ResultUserLogin : IComponentData
	{
		public NativeString64 Token;
		public int ClientId;
		public ulong UserId;
	}

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

	public struct CurrentMasterServerClient : IComponentData
	{
		/// NEVER SHARE IT
		public NativeString64 Token;
		public int ClientId;
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

		private Authentication.AuthenticationClient m_Client;

		private MasterServerRequestModule<RequestUserLogin, RequestUserLogin.Processing, ResultUserLogin> m_RequestUserLoginModule;
		private EntityQuery m_ClientQuery;
		
		private int m_RequestCount = 1;

		protected override void OnCreate()
		{
			base.OnCreate();
			
			GetModule(out m_RequestUserLoginModule);

			m_MasterServer              = World.GetOrCreateSystem<MasterServerSystem>();
			m_MasterServerWithoutClient = GetEntityQuery(typeof(MasterServerConnection), ComponentType.Exclude<UserAccountClient>());
			m_ClientQuery = GetEntityQuery(typeof(CurrentMasterServerClient));
			
			m_MasterServer.BeforeShutdown += OnBeforeShutdown;
		}

		protected override async void OnUpdate()
		{
			Entities.With(m_MasterServerWithoutClient).ForEach((Entity e, MasterServerConnection connection) =>
			{
				if (m_MasterServer.channel == null)
					return;
				
				m_Client = new Authentication.AuthenticationClient(m_MasterServer.channel);

				PostUpdateCommands.AddComponent(e, new UserAccountClient());
			});

			m_RequestUserLoginModule.Update();

			if (m_Client == null)
				return;
			
			var userLoginRequests = m_RequestUserLoginModule.GetRequests();
			foreach (var item in userLoginRequests)
			{
				var request = item.Value;
				var rpc = new UserLoginRequest
				{
					Login = request.Login.ToString(),
					Password = request.HashedPassword.ToString(),
					Type = request.Type
				};
				
				var result = await m_Client.UserLoginAsync(rpc);

				request.ErrorCode = result.Error;
				if (!request.error)
				{
					EntityManager.RemoveComponent<RequestUserLogin>(item.Entity);
					
					if (m_ClientQuery.CalculateLength() == 0)
					{
						Debug.Log("created entity...");
						EntityManager.CreateEntity(typeof(CurrentMasterServerClient));
					}
					SetSingleton(new CurrentMasterServerClient
					{
						ClientId = result.ClientId,
						Token    = new NativeString64(result.Token)
					});
				}

				EntityManager.AddComponentData(item.Entity, new ResultUserLogin
				{
					Token = new NativeString64(result.Token),
					ClientId = result.ClientId,
					UserId = result.UserId
				});
			}

			Entities.ForEach((ref RequestGetUserAccountData request) => { });
		}

		private void OnBeforeShutdown()
		{
			if (m_Client == null)
				return;
			
			Debug.Log("-1");
			if (m_ClientQuery.CalculateLength() > 0)
			{
				Debug.Log("-2");
				var data = m_ClientQuery.GetSingleton<CurrentMasterServerClient>();

				m_Client.DisconnectAsync(new DisconnectRequest
				{
					Reason = "shutdown",
					Token  = data.Token.ToString()
				});
				Debug.Log("-3");
			}
		}
	}
}