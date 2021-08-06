using GameHost;
using GameHost.Core.RPC.Interfaces;

namespace PataNext.Client.Rpc
{
	public struct UnitOverviewGetStatisticsMiniRpc : IGameHostRpcWithResponsePacket<UnitOverviewGetStatisticsMiniRpc.Response>
	{
		public const string RpcMethodName = "PataNext.Client.UnitOverview.GetStatisticsMini";

		public struct Response : IGameHostRpcResponsePacket
		{
			public string MethodName => RpcMethodName;

			public int   Health      { get; set; }
			public int   Defense     { get; set; }
			public int   Strength    { get; set; }
			public float AttackSpeed { get; set; }
		}

		public string MethodName => RpcMethodName;

		public GhGameEntitySafe TargetEntity { get; set; }
	}
}