using System.Collections.Generic;
using GameHost.Core.RPC.Interfaces;
using PataNext.Client.Systems;
using StormiumTeam.GameBase.Utility.Misc;

namespace PataNext.UnityCore.Rpc
{
	public struct GetDentComponentsRpc : IGameHostRpcWithResponsePacket<GetDentComponentsRpc.Response>
	{
		public const string RpcMethodName = "PataNext.GetDentComponents";

		public DentEntity Dent;

		public struct Response : IGameHostRpcResponsePacket
		{
			public Dictionary<string, string> ComponentTypeToJson;

			public string MethodName => RpcMethodName;
		}

		public string MethodName => RpcMethodName;
	}
	
	public struct ItemDetails
	{
		public ResPath Asset;
		public string  AssetType;
		public string  Name;
		public string  Description;

		public string ItemType;
	}
	
	public struct MissionDetails
	{
		public ResPath Path;
		public ResPath Scenar;
		public string  Name;
	}
}