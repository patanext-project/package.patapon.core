using GameHost.Core.RPC.Interfaces;
using StormiumTeam.GameBase.Utility.Misc;

namespace PataNext.UnityCore.Rpc
{
	public struct GetInventoryRpc : IGameHostRpcWithResponsePacket<GetInventoryRpc.Response>
	{
		public const string RpcMethodName = "PataNext.GetInventory";
		
		/// <summary>
		/// If true, it will only search for categories inside <see cref="FilterCategories"/>.
		/// If false, it will search for all except categories inside <see cref="FilterCategories"/>
		/// </summary>
		public bool FilterInclude;

		public string[] FilterCategories;

		public struct Response : IGameHostRpcResponsePacket
		{
			public struct Item
			{
				public MasterServerItemId Id;
				public ResPath            AssetResPath;

				public string             AssetType;
				public string             Name;
				public string             Description;
				public MasterServerUnitId EquippedBy;
			}

			public Item[] Items;

			public string MethodName => RpcMethodName;
		}

		public string MethodName => RpcMethodName;
	}
}