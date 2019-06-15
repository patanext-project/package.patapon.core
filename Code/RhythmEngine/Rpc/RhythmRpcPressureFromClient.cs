using package.patapon.core;
using package.patapon.def.Data;
using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.NetCode;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon4TLB.Default
{
	public struct RhythmRpcPressureFromClient : IRpcCommand
	{
		public int Key;
		public int Beat;
		public bool ShouldStartRecovery;

		public void Serialize(DataStreamWriter data)
		{
			data.Write(Key);
			data.Write(Beat);
			data.Write(ShouldStartRecovery ? 1 : 0);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			Key  = reader.ReadInt(ref ctx);
			Beat = reader.ReadInt(ref ctx);
			ShouldStartRecovery = reader.ReadInt(ref ctx) == 1;
		}

		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			var ent = commandBuffer.CreateEntity(jobIndex);

			commandBuffer.AddComponent(jobIndex, ent, new RhythmServerExecutePressure {Connection = connection, RpcData = this});
		}
	}

	public struct RhythmRpcPressureFromServer : IRpcCommand
	{
		public int   Key;
		public int   Beat;
		public float Score;
		public int   EngineGhostId;
		public bool  DoLocalEventOnSelf;

		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			var ent = commandBuffer.CreateEntity(jobIndex);

			commandBuffer.AddComponent(jobIndex, ent, new RhythmClientExecutePressure {Connection = connection, RpcData = this});
		}

		public void Serialize(DataStreamWriter writer)
		{
			writer.Write(Key);
			writer.Write(Beat);
			writer.Write(Score);
			writer.Write(EngineGhostId);
			writer.Write(DoLocalEventOnSelf ? (byte) 1 : (byte) 0);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			Key                = reader.ReadInt(ref ctx);
			Beat               = reader.ReadInt(ref ctx);
			Score              = reader.ReadFloat(ref ctx);
			EngineGhostId      = reader.ReadInt(ref ctx);
			DoLocalEventOnSelf = reader.ReadByte(ref ctx) == 1;
		}
	}

	public struct RhythmClientExecutePressure : IComponentData
	{
		public Entity Connection;

		public RhythmRpcPressureFromServer RpcData;
	}

	public struct RhythmServerExecutePressure : IComponentData
	{
		public Entity Connection;

		public RhythmRpcPressureFromClient RpcData;
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class RhythmClientCommandPressureSystem : JobComponentSystem
	{
		private struct Job : IJobForEachWithEntity<RhythmClientExecutePressure>
		{
			[ReadOnly]
			public ComponentDataFromEntity<RhythmEngineSimulateTag> SimulateTagFromEntity;

			[ReadOnly, NativeDisableContainerSafetyRestriction]
			public NativeHashMap<int, GhostEntity> GhostEntityMap;

			public EntityCommandBuffer.Concurrent CommandBuffer;

			[NativeDisableParallelForRestriction]
			public NativeList<FlowRhythmPressureEventProvider.Create> CreatePressureList;

			public void Execute(Entity entity, int index, ref RhythmClientExecutePressure executePressure)
			{
				if (GhostEntityMap.TryGetValue(executePressure.RpcData.EngineGhostId, out var ghostEntity)
				    // if we allow events on our engines, accept this condition
				    // if we don't allow events on our engine, but it's not an engine simulated by us, accept this condition
				    && (executePressure.RpcData.DoLocalEventOnSelf || !SimulateTagFromEntity.Exists(ghostEntity.entity)))
				{
					CreatePressureList.Add(new FlowRhythmPressureEventProvider.Create
					{
						Ev = new PressureEvent
						{
							Engine        = ghostEntity.entity,
							CorrectedBeat = executePressure.RpcData.Beat,
							OriginalBeat  = executePressure.RpcData.Beat,
							Key           = executePressure.RpcData.Key,
							Score         = executePressure.RpcData.Score
						}
					});
				}
				else if (ghostEntity.entity == default)
				{
					Debug.LogWarning("No ghost entity found with id=" + executePressure.RpcData.EngineGhostId);
				}

				CommandBuffer.DestroyEntity(index, entity);
			}
		}

		private EndPresentationEntityCommandBufferSystem m_Barrier;
		private FlowRhythmPressureEventProvider          m_PressureEventProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier               = World.GetOrCreateSystem<EndPresentationEntityCommandBufferSystem>();
			m_PressureEventProvider = World.GetOrCreateSystem<FlowRhythmPressureEventProvider>();

			GetEntityQuery(typeof(RhythmClientExecutePressure));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				SimulateTagFromEntity = GetComponentDataFromEntity<RhythmEngineSimulateTag>(),
				GhostEntityMap        = World.GetExistingSystem<GhostReceiveSystemGroup>().GhostEntityMap,
				CommandBuffer         = m_Barrier.CreateCommandBuffer().ToConcurrent(),
				CreatePressureList    = m_PressureEventProvider.GetEntityDelayedList()
			}.Schedule(this, inputDeps);

			m_PressureEventProvider.AddJobHandleForProducer(inputDeps);
			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}

	[UpdateBefore(typeof(RhythmEngineGroup))]
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class RhythmServerCommandPressureSystem : JobComponentSystem
	{
		private struct Job : IJobForEachWithEntity<RhythmServerExecutePressure>
		{
			[DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> EngineChunks;

			[ReadOnly] public ArchetypeChunkEntityType                           EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<Owner>                 OwnerType;
			[ReadOnly] public ComponentDataFromEntity<NetworkOwner>              NetworkOwnerFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GhostSystemStateComponent> GhostStateFromEntity;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingDataFromEntity;

			[ReadOnly] public ComponentDataFromEntity<RhythmEngineProcess> ProcessFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<RhythmEngineState>   StateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GameCommandState>    CommandStateFromEntity;

			[ReadOnly, DeallocateOnJobCompletion]
			public NativeArray<Entity> ConnectionEntities;

			public RpcQueue<RhythmRpcPressureFromServer> RpcQueue;

			public EntityCommandBuffer.Concurrent CommandBuffer;

			public void Execute(Entity eventEntity, int jobIndex, ref RhythmServerExecutePressure executePressure)
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

						var engine  = EngineChunks[chunk].GetNativeArray(EntityType)[ent];
						var process = ProcessFromEntity[engine];
						var state   = StateFromEntity[engine];
						var command = CommandStateFromEntity[engine];

						if (command.IsActive || executePressure.RpcData.ShouldStartRecovery)
						{
							// recover...
							Debug.Log($"recover... {command.IsActive} or {executePressure.RpcData.ShouldStartRecovery}");
							state.NextBeatRecovery  = process.Beat + 1;
						}

						state.LastPressureBeat = process.Beat;
						StateFromEntity[engine] = state;

						// When the client will send a command event, we will be able to check if the command is valid or not (if he used cheats)
						var bufferedEntity = CommandBuffer.CreateEntity(jobIndex);
						CommandBuffer.AddComponent(jobIndex, bufferedEntity, new PressureEvent
						{
							Engine = EngineChunks[chunk].GetNativeArray(EntityType)[ent],
							Key    = executePressure.RpcData.Key
						});

						for (var con = 0; con != ConnectionEntities.Length; con++)
						{
							RpcQueue.Schedule(OutgoingDataFromEntity[ConnectionEntities[con]], new RhythmRpcPressureFromServer
							{
								Beat               = -1,
								DoLocalEventOnSelf = false,
								EngineGhostId      = GhostStateFromEntity[EngineChunks[chunk].GetNativeArray(EntityType)[ent]].ghostId,
								Key                = executePressure.RpcData.Key
							});
						}

						break;
					}
				}

				CommandBuffer.DestroyEntity(jobIndex, eventEntity);
			}
		}

		private EntityQuery m_EngineQuery;
		private EntityQuery m_ConnectionQuery;

		private RhythmEngineBeginBarrier m_Barrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Barrier         = World.GetOrCreateSystem<RhythmEngineBeginBarrier>();
			m_ConnectionQuery = GetEntityQuery(typeof(NetworkStreamInGame), ComponentType.Exclude<NetworkStreamDisconnected>());
			m_EngineQuery     = GetEntityQuery(typeof(ShardRhythmEngine), typeof(RhythmEngineSettings), typeof(Owner));

			RequireForUpdate(GetEntityQuery(typeof(RhythmServerExecutePressure)));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_ConnectionQuery.AddDependency(inputDeps);

			inputDeps = new Job
			{
				EngineChunks           = m_EngineQuery.CreateArchetypeChunkArray(Allocator.TempJob),
				EntityType             = GetArchetypeChunkEntityType(),
				OwnerType              = GetArchetypeChunkComponentType<Owner>(true),
				NetworkOwnerFromEntity = GetComponentDataFromEntity<NetworkOwner>(true),
				GhostStateFromEntity   = GetComponentDataFromEntity<GhostSystemStateComponent>(true),
				ProcessFromEntity      = GetComponentDataFromEntity<RhythmEngineProcess>(true),
				CommandStateFromEntity = GetComponentDataFromEntity<GameCommandState>(true),
				StateFromEntity        = GetComponentDataFromEntity<RhythmEngineState>(false),
				CommandBuffer          = m_Barrier.CreateCommandBuffer().ToConcurrent(),

				ConnectionEntities     = m_ConnectionQuery.ToEntityArray(Allocator.TempJob, out var queryHandle),
				OutgoingDataFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>(),
				RpcQueue               = World.GetExistingSystem<RpcQueueSystem<RhythmRpcPressureFromServer>>().GetRpcQueue()
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, queryHandle));

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}