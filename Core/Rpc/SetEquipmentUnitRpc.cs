using System.Collections.Generic;
using GameHost;
using GameHost.Core.RPC.Interfaces;
using PataNext.Client.Systems;
using StormiumTeam.GameBase.Utility.Misc;

namespace PataNext.UnityCore.Rpc
{
	public struct SetEquipmentUnit : IGameHostRpcPacket
	{
		public const string RpcMethodName = "PataNext.SetEquipmentUnit";

		public GhGameEntitySafe               UnitEntity;
		public Dictionary<string, DentEntity> Targets;

		public string MethodName => RpcMethodName;
	}
}