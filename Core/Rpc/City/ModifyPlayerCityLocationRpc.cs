using GameHost;
using GameHost.Core.RPC.Interfaces;

namespace PataNext.Client.Rpc.City
{
	public struct ModifyPlayerCityLocationRpc : IGameHostRpcPacket
	{
		public GhGameEntitySafe LocationEntity;

		public string MethodName => "PataNext.Client.City.ModifyPlayerCityLocation";
	}
}