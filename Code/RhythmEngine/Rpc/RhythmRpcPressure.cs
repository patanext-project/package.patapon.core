using Unity.NetCode;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct RhythmRpcPressure : IRpcCommand
	{
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

			commandBuffer.AddComponent(jobIndex, ent, new RhythmExecutePressure {Connection = connection, Key = Key, Beat = Beat});
		}
	}

	public struct RhythmExecutePressure : IComponentData
	{
		public Entity Connection;
		
		public int Key;
		public int Beat;
	}
	
	public class RhythmCommandPressureSystem : JobComponentSystem
	{
		private struct Job : IJobForEach<RhythmExecutePressure>
		{
			public void Execute(ref RhythmExecutePressure c0)
			{
				
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job().Schedule(this, inputDeps);
		}
	}
}