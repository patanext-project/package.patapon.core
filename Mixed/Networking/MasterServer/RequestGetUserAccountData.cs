using P4TLB.MasterServer;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	public struct RequestGetUserAccountData : IMasterServerRequest, IComponentData
	{
		public struct Processing : IComponentData
		{
		}

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
}