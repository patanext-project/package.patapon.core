using package.patapon.core;
using package.patapon.def.Data;
using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.NetCode;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

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
			Debug.Log($"RhythmRpcPressure -> c:{connection} k:{Key} b:{Beat}");
			
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

	[UpdateBefore(typeof(RhythmEngineGroup))]
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class RhythmCommandPressureSystem : JobComponentSystem
	{
		private struct Job : IJobForEachWithEntity<RhythmExecutePressure>
		{
			[DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> EngineChunks;

			[ReadOnly] public            ArchetypeChunkEntityType              EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<Owner>    OwnerType;
			[ReadOnly] public ComponentDataFromEntity<NetworkOwner> NetworkOwnerFromEntity;

			public EntityCommandBuffer.Concurrent CommandBuffer;

			public void Execute(Entity eventEntity, int jobIndex, ref RhythmExecutePressure executePressure)
			{
				for (var chunk = 0; chunk != EngineChunks.Length; chunk++)
				{
					var count      = EngineChunks[chunk].Count;
					var ownerArray = EngineChunks[chunk].GetNativeArray(OwnerType);
					for (var ent = 0; ent != count; ent++)
					{
						if (!NetworkOwnerFromEntity.Exists(ownerArray[ent].Target))
							continue;
						var targetConnectionEntity = NetworkOwnerFromEntity[ownerArray[ent].Target].Value;
						if (targetConnectionEntity != executePressure.Connection)
							continue;

						// When the client will send a command event, we will be able to check if the command is valid or not (if he used cheats)
						var bufferedEntity = CommandBuffer.CreateEntity(jobIndex);
						CommandBuffer.AddComponent(jobIndex, bufferedEntity, new PressureEvent
						{
							Engine = EngineChunks[chunk].GetNativeArray(EntityType)[ent],
							Key    = executePressure.Key
						});

						break;
					}
				}

				CommandBuffer.DestroyEntity(jobIndex, eventEntity);
			}
		}

		private EntityQuery              m_EngineQuery;
		private RhythmEngineBeginBarrier m_Barrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier     = World.GetOrCreateSystem<RhythmEngineBeginBarrier>();
			m_EngineQuery = GetEntityQuery(typeof(ShardRhythmEngine), typeof(DefaultRhythmEngineSettings), typeof(Owner));

			RequireForUpdate(GetEntityQuery(typeof(RhythmExecutePressure)));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				EngineChunks           = m_EngineQuery.CreateArchetypeChunkArray(Allocator.TempJob),
				EntityType             = GetArchetypeChunkEntityType(),
				OwnerType              = GetArchetypeChunkComponentType<Owner>(true),
				NetworkOwnerFromEntity = GetComponentDataFromEntity<NetworkOwner>(true),
				CommandBuffer          = m_Barrier.CreateCommandBuffer().ToConcurrent()
			}.Schedule(this, inputDeps);

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}