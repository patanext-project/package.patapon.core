using package.patapon.core;
using package.patapon.def.Data;
using package.stormiumteam.shared;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon4TLB.Default
{
	public struct RhythmRpcClientRecover : IRpcCommand
	{
		public bool ForceRecover;
		public bool LooseChain;

		public void Execute(Entity connection, EntityCommandBuffer.Concurrent commandBuffer, int jobIndex)
		{
			var ent = commandBuffer.CreateEntity(jobIndex);
			commandBuffer.AddComponent(jobIndex, ent, new RhythmServerExecuteClientRecover
			{
				Connection   = connection,
				ForceRecover = ForceRecover,
				LooseChain   = LooseChain,
			});
		}

		public void Serialize(DataStreamWriter writer)
		{
			byte mask = 0, pos = 0;
			MainBit.SetBitAt(ref mask, pos++, ForceRecover);
			MainBit.SetBitAt(ref mask, pos++, LooseChain);

			writer.Write(mask);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			var mask = reader.ReadByte(ref ctx);
			{
				var pos = 0;
				ForceRecover = MainBit.GetBitAt(mask, pos++) == 1;
				LooseChain   = MainBit.GetBitAt(mask, pos++) == 1;
			}
		}
	}

	public struct RhythmServerExecuteClientRecover : IComponentData
	{
		public Entity Connection;
		public bool   ForceRecover;
		public bool   LooseChain;
	}

	[UpdateBefore(typeof(RhythmEngineGroup))]
	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class RhythmServerClientRecoverSystem : JobComponentSystem
	{
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<RhythmServerExecuteClientRecover>
		{
			[DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> EngineChunks;

			[ReadOnly] public ArchetypeChunkEntityType              EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<Owner>    OwnerType;
			[ReadOnly] public ComponentDataFromEntity<NetworkOwner> NetworkOwnerFromEntity;

			[ReadOnly]                            public ComponentDataFromEntity<RhythmEngineProcess>  ProcessFromEntity;
			[ReadOnly]                            public ComponentDataFromEntity<RhythmEngineSettings> SettingsFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<RhythmEngineState>    StateFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GameComboState>       ComboFromEntity;

			public EntityCommandBuffer.Concurrent CommandBuffer;

			public void Execute(Entity eventEntity, int jobIndex, ref RhythmServerExecuteClientRecover ev)
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
						if (targetConnectionEntity != ev.Connection)
							continue;

						var engine   = EngineChunks[chunk].GetNativeArray(EntityType)[ent];
						var process  = ProcessFromEntity[engine];
						var settings = SettingsFromEntity[engine];
						var state    = StateFromEntity[engine];
						var combo    = ComboFromEntity[engine];

						var flowBeat = process.GetFlowBeat(settings.BeatInterval);

						if (ev.ForceRecover)
						{
							state.NextBeatRecovery = flowBeat + 1;
						}

						if (ev.LooseChain)
						{
							combo.Chain        = 0;
							combo.Score        = 0;
							combo.IsFever      = false;
							combo.JinnEnergy   = 0;
							combo.ChainToFever = 0;
						}

						StateFromEntity[engine] = state;
						ComboFromEntity[engine] = combo;

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
				ComboFromEntity        = GetComponentDataFromEntity<GameComboState>(false),
				CommandBuffer          = m_Barrier.CreateCommandBuffer().ToConcurrent(),
			}.Schedule(this, inputDeps);

			m_Barrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}