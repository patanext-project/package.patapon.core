using GameHost;
using GameHost.Core.RPC.Interfaces;
using PataNext.Client.Systems;
using PataNext.UnityCore.Rpc;
using StormiumTeam.GameBase.Utility.Misc;

namespace PataNext.Client.Rpc
{
	public struct UnitOverviewGetRestrictedItemInventory : IGameHostRpcWithResponsePacket<UnitOverviewGetRestrictedItemInventory.Response>
	{
		private const string RpcMethodName = "PataNext.Client.UnitOverview.GetRestrictedItemInventory";

		public GhGameEntitySafe EntityTarget;
		public string           AttachmentTarget;

		public struct Response : IGameHostRpcResponsePacket
		{
			public string MethodName => RpcMethodName;

			public struct Item
			{
				public DentEntity Id;
				public ResPath    AssetResPath;

				public string             AssetType;
				public string             Name;
				public string             Description;
				public MasterServerUnitId EquippedBy;
			}

			public Item[] Items;
		}

		public string MethodName => RpcMethodName;
	}
}