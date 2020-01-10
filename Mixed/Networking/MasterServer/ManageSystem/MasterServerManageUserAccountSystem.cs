using P4TLB.MasterServer;
using Patapon4TLB.Core.MasterServer.Implementations;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServer
{
	public struct RequestUserLogin : IMasterServerRequest, IComponentData
	{
		public struct Processing : IComponentData
		{
		}

		public UserLoginResponse.Types.ErrorCode ErrorCode;

		public bool error => ErrorCode != UserLoginResponse.Types.ErrorCode.Success;

		public NativeString64                     Login;
		public NativeString512                    HashedPassword;
		public NativeString512                    RoutedData;
		public UserLoginRequest.Types.RequestType Type;

		public RequestUserLogin(string login, string password, UserLoginRequest.Types.RequestType type, string routedData = "")
		{
			Login          = new NativeString64(login);
			HashedPassword = new NativeString512(password);
			RoutedData     = new NativeString512(routedData);
			Type           = type;

			ErrorCode = UserLoginResponse.Types.ErrorCode.Invalid;
		}
	}

	public struct ResultUserLogin : IComponentData
	{
		public NativeString64 Token;
		public int            ClientId;
		public ulong          UserId;
	}
	
	public class MasterServerManageUserAccountSystem : BaseSystemMasterServerService
	{
		private EntityQuery                                                                               m_ClientQuery;
		private MasterServerRequestModule<RequestUserLogin, RequestUserLogin.Processing, ResultUserLogin> m_RequestUserLoginModule;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_RequestUserLoginModule);
			m_ClientQuery = GetEntityQuery(typeof(ConnectedMasterServerClient));
		}

		protected override async void OnUpdate()
		{
			if (!StaticMasterServer.HasClient<AuthenticationService.AuthenticationServiceClient>())
				return;

			var client = StaticMasterServer.GetClient<AuthenticationService.AuthenticationServiceClient>();

			m_RequestUserLoginModule.Update();
			m_RequestUserLoginModule.AddProcessTagToAllRequests();

			var userLoginRequests = m_RequestUserLoginModule.GetRequests();
			foreach (var item in userLoginRequests)
			{
				var request = item.Value;
				var rpc = new UserLoginRequest
				{
					Login     = request.Login.ToString(),
					Password  = request.HashedPassword.ToString(),
					Type      = request.Type,
					RouteData = request.RoutedData.ToString()
				};

				var result = await client.UserLoginAsync(rpc);
				// if the user deleted the entity, throw an error as it's not accepted when log in...
				if (!EntityManager.Exists(item.Entity))
				{
					Debug.LogError("You shouldn't destroy the 'LogInRequest' entity.");
					continue;
				}

				request.ErrorCode = result.Error;
				Debug.Log("error? " + request.error);
				if (!request.error)
				{
					EntityManager.RemoveComponent<RequestUserLogin>(item.Entity);

					var isType = request.Type == UserLoginRequest.Types.RequestType.Player ? typeof(MasterServerIsPlayer) : typeof(MasterServerIsServer);
					var ent    = EntityManager.CreateEntity(typeof(ConnectedMasterServerClient), isType);
					EntityManager.SetComponentData(ent, new ConnectedMasterServerClient
					{
						ClientId  = result.ClientId,
						UserId    = result.UserId,
						UserLogin = request.Login,

						Token = new NativeString64(result.Token)
					});

					Debug.Log("Creating: " + ent + ", " + World);
				}

				EntityManager.AddComponentData(item.Entity, new ResultUserLogin
				{
					Token    = new NativeString64(result.Token),
					ClientId = result.ClientId,
					UserId   = result.UserId
				});
			}
		}

		protected override void OnShutdown()
		{
			if (!StaticMasterServer.HasClient<AuthenticationService.AuthenticationServiceClient>())
				return;

			var client = StaticMasterServer.GetClient<AuthenticationService.AuthenticationServiceClient>();

			using (var entities = m_ClientQuery.ToEntityArray(Allocator.TempJob))
			using (var components = m_ClientQuery.ToComponentDataArray<ConnectedMasterServerClient>(Allocator.TempJob))
			{
				for (var ent = 0; ent != entities.Length; ent++)
				{
					client.DisconnectAsync(new DisconnectRequest
					{
						Reason = "shutdown",
						Token  = components[ent].Token.ToString()
					});

					EntityManager.DestroyEntity(entities[ent]);
				}
			}
		}
	}
}