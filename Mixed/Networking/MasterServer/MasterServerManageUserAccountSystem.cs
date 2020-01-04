using P4TLB.MasterServer;
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
		public UserLoginRequest.Types.RequestType Type;

		public RequestUserLogin(string login, string password, UserLoginRequest.Types.RequestType type)
		{
			Login          = new NativeString64(login);
			HashedPassword = new NativeString512(password);
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

	[UpdateInGroup(typeof(MasterServerProcessRpcSystem))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
	[AlwaysUpdateSystem]
	public class MasterServerManageUserAccountSystem : GameBaseSystem
	{
		private EntityQuery                          m_ClientQuery;
		private MasterServerSystem                   m_MasterServer;
		private MasterServerRequestUserAccountSystem m_RequestSystem;

		private MasterServerRequestModule<RequestUserLogin, RequestUserLogin.Processing, ResultUserLogin> m_RequestUserLoginModule;

		protected override void OnCreate()
		{
			base.OnCreate();

			/* todo: if (World != BootWorld.World)
				throw new InvalidOperationException();*/

			GetModule(out m_RequestUserLoginModule);

			m_MasterServer  = World.GetOrCreateSystem<MasterServerSystem>();
			m_RequestSystem = World.GetOrCreateSystem<MasterServerRequestUserAccountSystem>();
			m_ClientQuery   = GetEntityQuery(typeof(ConnectedMasterServerClient));

			m_MasterServer.BeforeShutdown += OnBeforeShutdown;
		}

		protected override async void OnUpdate()
		{
			if (m_RequestSystem.Client == null)
				return;

			m_RequestUserLoginModule.Update();
			m_RequestUserLoginModule.AddProcessTagToAllRequests();

			var userLoginRequests = m_RequestUserLoginModule.GetRequests();
			foreach (var item in userLoginRequests)
			{
				var request = item.Value;
				var rpc = new UserLoginRequest
				{
					Login    = request.Login.ToString(),
					Password = request.HashedPassword.ToString(),
					Type     = request.Type
				};

				var result = await m_RequestSystem.Client.UserLoginAsync(rpc);
				// if the user deleted the entity, throw an error as it's not accepted when log in...
				if (!EntityManager.Exists(item.Entity))
				{
					Debug.LogError("You shouldn't destroy the 'LogInRequest' entity.");
					continue;
				}

				request.ErrorCode = result.Error;
				if (!request.error)
				{
					EntityManager.RemoveComponent<RequestUserLogin>(item.Entity);

					var isType = request.Type == UserLoginRequest.Types.RequestType.Player ? typeof(MasterServerIsPlayer) : typeof(MasterServerIsServer);
					var ent    = EntityManager.CreateEntity(typeof(ConnectedMasterServerClient), isType);
					EntityManager.SetComponentData(ent, new ConnectedMasterServerClient
					{
						ClientId = result.ClientId,
						Token    = new NativeString64(result.Token)
					});
				}

				EntityManager.AddComponentData(item.Entity, new ResultUserLogin
				{
					Token    = new NativeString64(result.Token),
					ClientId = result.ClientId,
					UserId   = result.UserId
				});
			}
		}

		private void OnBeforeShutdown()
		{
			if (m_RequestSystem.Client == null)
				return;

			using (var entities = m_ClientQuery.ToEntityArray(Allocator.TempJob))
			using (var components = m_ClientQuery.ToComponentDataArray<ConnectedMasterServerClient>(Allocator.TempJob))
			{
				for (var ent = 0; ent != entities.Length; ent++)
				{
					m_RequestSystem.Client.DisconnectAsync(new DisconnectRequest
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