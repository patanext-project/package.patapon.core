using System.Net;
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
	public class TestGetUserLogin : ComponentSystem
	{
		// Tag to differentiate our custom request from other requests.
		private struct CustomRequestTag : IComponentData
		{}
		
		private EntityQuery m_ResultQuery;
		
		protected override void OnCreate()
		{
			var masterServerSystem = World.GetOrCreateSystem<MasterServerSystem>();
			// Set the target of our MasterServer here
			masterServerSystem.SetMasterServer(new IPEndPoint(IPAddress.Any, 42));
			
			// Create our request.
			// The 'RequestUserAccountData' component will be removed once the MasterServer has sent an answer to us (it will add a 'ResultUserAccountData' component)
			var request = EntityManager.CreateEntity(typeof(CustomRequestTag), typeof(RequestUserAccountData));
			// get from an user that got a GUID of 0 (that an example, GUID don't work like that lol)
			EntityManager.SetComponentData(request, new RequestUserAccountData {UserGuid = 0}); 
			
			// Create a query where we only want the result.
			// If there is a result, then there shouldn't be any Request* component anymore on it.
			m_ResultQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(CustomRequestTag), typeof(ResultUserAccountData)},
				None = new ComponentType[] {typeof(RequestUserAccountData)}
			});
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((ref RequestUserAccountData request) =>
			{
				if (request.error)
				{
					Debug.Log("error!");
				}
			});
			
			Entities.With(m_ResultQuery).ForEach((Entity e, ref ResultUserAccountData result) =>
			{
				Debug.Log($"Account data received! login={result.Login}");

				// Destroy our entity...
				PostUpdateCommands.DestroyEntity(e);
			});
		}

		protected override void OnDestroy()
		{
			// Disconnect from the MasterServer... (it will be automatic in MasterServerSystem)
			World.GetExistingSystem<MasterServerSystem>().Disconnect();
		}
	}
}