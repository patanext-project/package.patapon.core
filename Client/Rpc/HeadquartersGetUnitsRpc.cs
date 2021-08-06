using GameHost;
using GameHost.Core.RPC.Interfaces;

namespace PataNext.Simulation.Client.Rpc
{
	public struct HeadquartersGetUnitsRpc : IGameHostRpcWithResponsePacket<HeadquartersGetUnitsRpc.Response>
	{
		public const string RpcMethodName = "PataNext.Client.Headquarters.GetUnits";
		
		public struct Response : IGameHostRpcResponsePacket
		{
			public enum ESquadType
			{
				Standard,
				Hatapon,
				Player,
			}

			public struct Squad
			{
				public ESquadType Type;

				public GhGameEntitySafe   Leader;
				public GhGameEntitySafe[] Soldiers;
			}

			public Squad[] Squads;

			public string MethodName => RpcMethodName;
		}

		public string MethodName => RpcMethodName;
	}
}