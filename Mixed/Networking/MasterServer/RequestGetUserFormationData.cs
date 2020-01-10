using System.Collections.Generic;
using P4TLB.MasterServer;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	public struct RequestGetUserFormationData : IMasterServerRequest, IComponentData
	{
		public bool error => ErrorCode != 0;

		public ulong          UserId;
		public NativeString64 UserLogin;

		public int ErrorCode;
		
		public struct Processing : IComponentData {}
	}

	public class ResultGetUserFormationData : IComponentData
	{
		public P4ArmyFormationRoot Root;
		public IEnumerable<P4Army> Armies => Root.Armies;
	}
}