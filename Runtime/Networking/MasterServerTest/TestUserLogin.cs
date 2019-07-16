using System;
using System.Net;
using P4TLB.MasterServer;
using Patapon4TLB.Core.MasterServer;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Core.MasterServerTest
{
	/* BEHAVIOR OF THIS TEST:
	 * 
	 * Connect to the MasterServer
	 * Send a request to the MasterServer to get the UserData from an user GUID
	 * Wait until we the result of our request... (it's asynchronous)
	 * Once we get the result, print it here and destroy it
	 * 
	 */
	public class TestUserLogin : ComponentSystem
	{
		// Tag to differentiate our custom request from other requests.
		private struct CustomRequestTag : IComponentData
		{}
		
		private EntityQuery m_ResultQuery;
		
		protected override void OnCreate()
		{
			var masterServerSystem = World.GetOrCreateSystem<MasterServerSystem>();
			// Set the target of our MasterServer here
			masterServerSystem.SetMasterServer(new IPEndPoint(IPAddress.Loopback, 4242));
			
			// Create our request.
			// The 'RequestUserAccountData' component will be removed once the MasterServer has sent an answer to us (it will add a 'ResultUserAccountData' component)
			var request = EntityManager.CreateEntity(typeof(CustomRequestTag), typeof(RequestUserLogin));
			EntityManager.SetComponentData(request, new RequestUserLogin("DISCORD_136793316523114497", string.Empty, UserLoginRequest.Types.RequestType.Player)); 
			
			// Create a query where we only want the result.
			// If there is a result and no error, then there shouldn't be any Request* component anymore on it.
			m_ResultQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(CustomRequestTag), typeof(ResultUserLogin)},
				None = new ComponentType[] {typeof(RequestUserLogin)}
			});
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((ref RequestGetUserAccountData request, ref ResultUserLogin result) =>
			{
				if (request.error)
				{
					Debug.Log("error!");
				}
			});
			
			Entities.With(m_ResultQuery).ForEach((Entity e, ref ResultUserLogin result) =>
			{
				Debug.Log($"Account data received! token={result.Token}, clientId={result.ClientId}, userId={result.UserId}");

				// Destroy our entity...
				PostUpdateCommands.DestroyEntity(e);
			});
		}
	}
}