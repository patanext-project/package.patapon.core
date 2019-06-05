using System.Security.Cryptography.X509Certificates;
using package.stormiumteam.networking.runtime.Rpc;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct RhythmRpcPressure : IRpcBase<RhythmRpcPressure>
	{
		public RpcBase.Header Header { get; set; }

		public int Key;
		public int Beat;

		public void Serialize(DataStreamWriter data)
		{
			data.Write(Key);
			data.Write(Beat);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			Key  = reader.ReadInt(ref ctx);
			Beat = reader.ReadInt(ref ctx);
		}

		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			var ent = commandBuffer.CreateEntity(jobIndex);

			commandBuffer.AddComponent(jobIndex, ent, new RhythmCommandPressure {Connection = connection, Key = Key, Beat = Beat});
		}
	}

	public struct RhythmCommandPressure : IComponentData
	{
		public Entity Connection;
		
		public int Key;
		public int Beat;
	}
}