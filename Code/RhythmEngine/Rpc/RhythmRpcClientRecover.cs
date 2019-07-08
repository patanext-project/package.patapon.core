using package.patapon.core;
using package.patapon.def.Data;
using Runtime.EcsComponents;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct RhythmRpcClientRecover : IRpcCommand
	{
		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			var ent = commandBuffer.CreateEntity(jobIndex);
			commandBuffer.AddComponent(jobIndex, ent, new RhythmServerExecuteClientRecover
			{
				Connection = connection
			});
		}

		public void Serialize(DataStreamWriter writer)
		{

		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{

		}
	}

	public struct RhythmServerExecuteClientRecover : IComponentData
	{
		public Entity Connection;
	}

	[UpdateBefore(typeof(RhythmEngineGroup))]
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class RhythmServerClientRecoverSystem : JobComponentSystem
	{
		private struct Job : IJobForEachWithEntity<RhythmServerExecuteClientRecover>
		{
			[DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> EngineChunks;

			[ReadOnly] public ArchetypeChunkEntityType              EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<Owner>    OwnerType;
			[ReadOnly] public ComponentDataFromEntity<NetworkOwner> NetworkOwnerFromEntity;

			[ReadOnly]                            public ComponentDataFromEntity<RhythmEngineProcess>  ProcessFromEntity;
			[ReadOnly]                            public ComponentDataFromEntity<RhythmEngineSettings> SettingsFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<RhythmEngineState>    StateFromEntity;

			public EntityCommandBuffer.Concurrent CommandBuffer;

			public void Execute(Entity eventEntity, int jobIndex, ref RhythmServerExecuteClientRecover executePressure)
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

						var engine   = EngineChunks[chunk].GetNativeArray(EntityType)[ent];
						var process  = ProcessFromEntity[engine];
						var settings = SettingsFromEntity[engine];
						var state    = StateFromEntity[engine];

						var flowBeat = process.GetFlowBeat(settings.BeatInterval);

						state.NextBeatRecovery = flowBeat + 1;

						StateFromEntity[engine] = state;

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
			m_EngineQuery = GetEntityQuery(typeof(ShardRhythmEngine), typeof(RhythmEngineSettings), typeof(Owner));

			RequireForUpdate(GetEntityQuery(typeof(RhythmServerExecuteClientRecover)));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				EngineChunks           = m_EngineQuery.CreateArchetypeChunkArray(Allocator.TempJob),
				EntityType             = GetArchetypeChunkEntityType(),
				OwnerType              = GetArchetypeChunkComponentType<Owner>(true),
				NetworkOwnerFromEntity = GetComponentDataFromEntity<NetworkOwner>(true),
				ProcessFromEntity      = GetComponentDataFromEntity<RhythmEngineProcess>(true),
				SettingsFromEntity     = GetComponentDataFromEntity<RhythmEngineSettings>(true),
				StateFromEntity        = GetComponentDataFromEntity<RhythmEngineState>(false),
				CommandBuffer          = m_Barrier.CreateCommandBuffer().ToConcurrent(),
			}.Schedule(this, inputDeps);

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}