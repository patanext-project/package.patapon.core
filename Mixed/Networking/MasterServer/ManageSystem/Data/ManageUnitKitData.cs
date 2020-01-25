using P4TLB.MasterServer;
using Unity.Collections;
using Unity.Entities;

namespace Patapon4TLB.Core.MasterServer.Data
{
	public struct RequestGetUnitKit : IComponentData
	{
		public ulong UnitId;

		public struct Automatic : IAutomaticRequestComponent<RequestGetUnitKit>
		{
			public GetKitResponse.Types.ErrorCode ErrorCode;
			public ulong                          UnitId;

			public void SetRequest(ref RequestGetUnitKit original)
			{
				original.UnitId = UnitId;
			}

			public bool error => ErrorCode != GetKitResponse.Types.ErrorCode.Ok;
		}

		public struct PushRequest : IComponentData
		{
		}

		public struct Processing : IComponentData
		{
		}

		public struct CompletionStatus : IRequestCompletionStatus
		{
			public bool error => ErrorCode != GetKitResponse.Types.ErrorCode.Ok;

			public GetKitResponse.Types.ErrorCode ErrorCode;
		}
	}

	public struct ResponseGetUnitKit : IComponentData
	{
		public P4OfficialKit   KitId;
		public NativeString128 KitCustomNameId;
	}

	public struct RequestSetUnitKit : IComponentData
	{
		public ulong           UnitId;
		public P4OfficialKit   KitId;
		public NativeString128 KitCustomNameId;

		public struct Processing : IComponentData
		{
		}

		public struct CompletionStatus : IRequestCompletionStatus
		{
			public bool error => ErrorCode != SetKitResponse.Types.ErrorCode.Ok;

			public SetKitResponse.Types.ErrorCode ErrorCode;

		}
	}

	public struct ResponseSetUnitKit : IComponentData
	{

	}
}