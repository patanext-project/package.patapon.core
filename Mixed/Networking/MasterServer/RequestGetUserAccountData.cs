using P4TLB.MasterServer;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Entities;

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
		public AuthenticationService.AuthenticationServiceClient Client;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_MasterServer = BootWorld.GetOrCreateSystem<MasterServerSystem>();
		}

		protected override void OnUpdate()
		{
			if (m_MasterServer.channel != null
			    && !m_MasterServer.HasClient<AuthenticationService.AuthenticationServiceClient>())
			{
				m_MasterServer.AddClient(() => { return Client = new AuthenticationService.AuthenticationServiceClient(m_MasterServer.channel); });
			}
		}
	}
}