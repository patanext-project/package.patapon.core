using GameHost.Core.RPC.Interfaces;
using StormiumTeam.GameBase.Utility.Misc;

namespace PataNext.Client.Rpc.City
{
	public struct LeaveMissionAndReturnToCityRpc : IGameHostRpcPacket
	{
		public string MethodName => "PataNext.Client.City.LeaveMissionAndReturnToCity";
	}
}