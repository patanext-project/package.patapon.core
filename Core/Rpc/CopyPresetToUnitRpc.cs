using GameHost.Core.RPC.Interfaces;
using PataNext.UnityCore.Rpc;

namespace PataNext.Simulation.Client.Rpc
{
	public struct CopyPresetToUnitRpc : IGameHostRpcPacket
	{
		public MasterServerUnitId       Unit;
		public MasterServerUnitPresetId Preset;
		
		public string MethodName => "PataNext.CopyPresetToUnit";
	}
}