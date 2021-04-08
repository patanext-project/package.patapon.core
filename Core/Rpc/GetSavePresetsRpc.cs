using GameHost.Core.RPC.Interfaces;
using StormiumTeam.GameBase.Utility.Misc;

namespace PataNext.UnityCore.Rpc
{
	public struct GetSavePresetsRpc : IGameHostRpcWithResponsePacket<GetSavePresetsRpc.Response>
	{
		public const string RpcMethodName = "PataNext.GetPresets";
		
		public MasterServerSaveId Save;

		public struct Response : IGameHostRpcResponsePacket
		{
			public struct Preset
			{
				public MasterServerUnitPresetId Id;
				public string                   Name;
				public string                   ArchetypeId;
				public string                   KitId;
				public string                   RoleId;
			}

			public Preset[] Presets;
			public string   MethodName => RpcMethodName;
		}

		public string MethodName => RpcMethodName;
	}
}