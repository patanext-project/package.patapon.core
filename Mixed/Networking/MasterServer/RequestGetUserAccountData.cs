using P4TLB.MasterServer;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	public struct RequestGetUserAccountData : IComponentData
	{
		public struct Processing : IComponentData
		{
		}

		public long UserGuid;

		public struct CompletionStatus : IRequestCompletionStatus
		{
			public bool error => ErrorCode != 0;
			public int  ErrorCode;
		}
	}

	public struct ResultGetUserAccountData : IComponentData
	{
		public NativeString64 Login;
	}
}