using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Patapon.Mixed.RhythmEngine.Rpc
{
	[BurstCompile]
	public struct RhythmRpcClientRecover : IRpcCommand
	{
		public bool ForceRecover;
		public int  RecoverBeat;

		public bool LooseChain;

		public void Serialize(DataStreamWriter writer)
		{
			byte mask = 0, pos = 0;
			MainBit.SetBitAt(ref mask, pos++, ForceRecover);
			MainBit.SetBitAt(ref mask, pos++, LooseChain);

			writer.Write(mask);
			writer.Write(RecoverBeat);
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			var mask = reader.ReadByte(ref ctx);
			{
				var pos = 0;
				ForceRecover = MainBit.GetBitAt(mask, pos++) == 1;
				LooseChain   = MainBit.GetBitAt(mask, pos++) == 1;
			}
			RecoverBeat = reader.ReadInt(ref ctx);
		}

		[BurstCompile]
		private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
		{
			RpcExecutor.ExecuteCreateRequestComponent<RhythmRpcClientRecover>(ref parameters);
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
		}

		public class RpcSystem : RpcCommandRequestSystem<RhythmRpcClientRecover>
		{
		}
	}

	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class RhythmClientRecoverManageSystem : JobComponentSystem
	{
		private EndSimulationEntityCommandBufferSystem m_EndBarrier;
		private EntityQuery                            m_EventQuery;
		private EntityQuery                            m_EngineQuery;

		protected override void OnCreate()
		{
			m_EngineQuery = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));
			m_EventQuery  = GetEntityQuery(typeof(RhythmRpcClientRecover));
			m_EndBarrier  = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override unsafe JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_EngineQuery.AddDependency(inputDeps);

			var engineChunks           = m_EngineQuery.CreateArchetypeChunkArray(Allocator.TempJob, out var queryHandle);
			var playerRelativeType     = GetArchetypeChunkComponentType<Relative<PlayerDescription>>(true);
			var networkOwnerFromEntity = GetComponentDataFromEntity<NetworkOwner>(true);
			var processType            = GetArchetypeChunkComponentType<FlowEngineProcess>(false);
			var settingsType           = GetArchetypeChunkComponentType<RhythmEngineSettings>(true);
			var stateType              = GetArchetypeChunkComponentType<RhythmEngineState>(false);
			var comboType              = GetArchetypeChunkComponentType<GameComboState>(false);

			var tick = World.GetExistingSystem<ServerSimulationSystemGroup>().ServerTick;

			inputDeps =
				Entities
					.ForEach((Entity entity, int nativeThreadIndex, in RhythmRpcClientRecover ev, in ReceiveRpcCommandRequestComponent receiveData) =>
					{
						for (var chunk = 0; chunk != engineChunks.Length; chunk++)
						{
							var count               = engineChunks[chunk].Count;
							var playerRelativeArray = engineChunks[chunk].GetNativeArray(playerRelativeType);
							var processArray        = engineChunks[chunk].GetNativeArray(processType);
							var settingsArray       = engineChunks[chunk].GetNativeArray(settingsType);
							var stateArray          = engineChunks[chunk].GetNativeArray(stateType);
							var comboArray          = engineChunks[chunk].GetNativeArray(comboType);
							for (var ent = 0; ent != count; ent++)
							{
								if (!networkOwnerFromEntity.Exists(playerRelativeArray[ent].Target))
									continue;
								var targetConnectionEntity = networkOwnerFromEntity[playerRelativeArray[ent].Target].Value;
								if (targetConnectionEntity != receiveData.SourceConnection)
									continue;

								ref var process = ref UnsafeUtilityEx.ArrayElementAsRef<FlowEngineProcess>(processArray.GetUnsafePtr(), ent);
								ref var combo   = ref UnsafeUtilityEx.ArrayElementAsRef<GameComboState>(comboArray.GetUnsafePtr(), ent);
								ref var state   = ref UnsafeUtilityEx.ArrayElementAsRef<RhythmEngineState>(stateArray.GetUnsafePtr(), ent);

								var flowBeat = process.GetFlowBeat(settingsArray[ent].BeatInterval);
								if (ev.ForceRecover)
								{
									state.RecoveryTick     = (int) tick;
									state.NextBeatRecovery = flowBeat + 1;
									if (ev.RecoverBeat > 0) // this condition should always be false if we don't enable 'UseClientSimulation' in settings
										state.NextBeatRecovery = ev.RecoverBeat;
								}

								if (ev.LooseChain)
								{
									combo.Chain        = 0;
									combo.Score        = 0;
									combo.IsFever      = false;
									combo.JinnEnergy   = 0;
									combo.ChainToFever = 0;
								}

								break;
							}
						}
					})
					.WithReadOnly(engineChunks)
					.WithReadOnly(playerRelativeType)
					.WithReadOnly(networkOwnerFromEntity)
					.WithReadOnly(settingsType)
					.Schedule(JobHandle.CombineDependencies(inputDeps, queryHandle));

			m_EndBarrier.CreateCommandBuffer().DestroyEntity(m_EventQuery);
			m_EndBarrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}