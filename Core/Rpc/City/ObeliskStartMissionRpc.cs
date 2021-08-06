using GameHost.Core.RPC.Interfaces;
using StormiumTeam.GameBase.Utility.Misc;

namespace PataNext.Client.Rpc.City
{
	public struct ObeliskStartMissionRpc : IGameHostRpcPacket
	{
		public string Path;

		public string MethodName => "PataNext.Client.City.ObeliskStartMission";
	}
}