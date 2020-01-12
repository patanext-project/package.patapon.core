using System.Collections.Generic;
using P4TLB.MasterServer;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	public struct RequestGetUserFormationData : IComponentData
	{
		public ulong          UserId;
		public NativeString64 UserLogin;

		public struct Processing : IComponentData
		{
		}

		public struct CompletionStatus : IRequestCompletionStatus
		{
			public bool                                       error => ErrorCode != 0;
			public GetFormationOfPlayerResult.Types.ErrorCode ErrorCode;
		}
	}

	public class ResultGetUserFormationData : IComponentData
	{
		public P4ArmyFormationRoot Root;
		public IEnumerable<P4Army> Armies => Root.Armies;
	}
}