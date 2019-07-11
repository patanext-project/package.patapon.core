using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer
{
	public struct RequestUserAccountData : IMasterServerRequest, IComponentData
	{
		public int ErrorCode; // enum?
		public bool error => ErrorCode != 0;

		public long UserGuid;
	}

	public struct ResultUserAccountData : IComponentData
	{
		public NativeString64 Login;
	}
}