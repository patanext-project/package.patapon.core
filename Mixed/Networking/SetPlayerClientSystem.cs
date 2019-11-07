using System;
using System.Security.Cryptography;
using System.Text;
using P4TLB.MasterServer;
using Patapon4TLB.Core.MasterServer;
using Unity.Entities;
using Revolution.NetCode;
using UnityEngine;

namespace Patapon4TLB.Core
{
	[NotClientServerSystem]
	public class SetPlayerClientSystem : ComponentSystem
	{
		private string m_Login;
		private string m_HashedPassword;

		private Entity m_PreviousRequest;

		protected override void OnCreate()
		{
			base.OnCreate();

			SetLoginAndPassword("server_0", "");
			RequestConnection();
		}

		protected override void OnUpdate()
		{
			if (m_PreviousRequest == default)
				return;

			if (!EntityManager.HasComponent<ResultUserLogin>(m_PreviousRequest))
				return;
			// error...
			if (EntityManager.HasComponent<RequestUserLogin>(m_PreviousRequest))
			{
				var error = EntityManager.GetComponentData<RequestUserLogin>(m_PreviousRequest).ErrorCode;
				Debug.LogError($"Error when trying to connect: {error}");

				EntityManager.DestroyEntity(m_PreviousRequest);
				m_PreviousRequest = default;
				return;
			}

			Debug.Log("Successfuly connected...");

			var result = EntityManager.GetComponentData<ResultUserLogin>(m_PreviousRequest);

			EntityManager.DestroyEntity(m_PreviousRequest);
			m_PreviousRequest = default;

			foreach (var world in ClientServerBootstrap.clientWorld)
			{
				var query = world.EntityManager.CreateEntityQuery(typeof(ConnectedMasterServerClient));
				if (query.CalculateEntityCount() == 0)
				{
					world.EntityManager.CreateEntity(typeof(ConnectedMasterServerClient));
				}

				query.SetSingleton(new ConnectedMasterServerClient
				{
					Token    = result.Token,
					ClientId = result.ClientId
				});
			}
		}

		public void SetLoginAndPassword(string login, string password)
		{
			m_Login          = login;
			m_HashedPassword = CalculateMD5Hash(password);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>The request entity</returns>
		public Entity RequestConnection()
		{
			if (m_Login == null || m_HashedPassword == null)
				throw new ArgumentNullException();

			var request = EntityManager.CreateEntity(typeof(RequestUserLogin));
			{
				EntityManager.SetComponentData(request, new RequestUserLogin
				{
					Login          = new NativeString64(m_Login),
					HashedPassword = new NativeString512(m_HashedPassword),
					Type           = UserLoginRequest.Types.RequestType.Player
				});
			}

			return m_PreviousRequest = request;
		}

		private string CalculateMD5Hash(string input)
		{
			// step 1, calculate MD5 hash from input
			var md5        = MD5.Create();
			var inputBytes = Encoding.ASCII.GetBytes(input);
			var hash       = md5.ComputeHash(inputBytes);

			// step 2, convert byte array to hex string
			var sb = new StringBuilder();
			foreach (var t in hash)
			{
				sb.Append(t.ToString("X2"));
			}

			return sb.ToString();
		}
	}
}