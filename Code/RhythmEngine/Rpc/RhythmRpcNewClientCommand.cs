using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct RhythmRpcNewClientCommand : IRpcCommand
	{
		public int Type;
		
		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			var ent = commandBuffer.CreateEntity(jobIndex);
			commandBuffer.AddComponent(jobIndex, ent, new RhythmExecuteCommand
			{
				Connection = connection,
				Type = Type
			});
		}

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(Type);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			Type = reader.ReadInt(ref ctx);
		}
	}

	public struct RhythmExecuteCommand : IComponentData
	{
		public Entity Connection;
		public int Type;
	}
}