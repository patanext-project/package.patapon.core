using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.EcsComponents;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon.Mixed.RhythmEngine.Rpc
{
	[BurstCompile]
	public struct RhythmRpcClientRecover : IRpcCommand
	{
		public uint EngineGhostId;

		public bool ForceRecover;
		public int  RecoverBeat;

		public bool LooseChain;

		public void Serialize(DataStreamWriter writer)
		{
			byte mask = 0, pos = 0;
			MainBit.SetBitAt(ref mask, pos++, ForceRecover);
			MainBit.SetBitAt(ref mask, pos++, LooseChain);

			writer.Write(mask);
			writer.Write(EngineGhostId);
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
			EngineGhostId = reader.ReadUInt(ref ctx);
			RecoverBeat   = reader.ReadInt(ref ctx);
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
	public class RhythmClientRecoverManageSystem : SystemBase
	{
		private CreateSnapshotSystem                   m_CreateSnapshotSystem;
		private EndSimulationEntityCommandBufferSystem m_EndBarrier;
		private EntityQuery                            m_EventQuery;

		protected override void OnCreate()
		{
			m_EventQuery           = GetEntityQuery(typeof(RhythmRpcClientRecover));
			m_EndBarrier           = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
			m_CreateSnapshotSystem = World.GetOrCreateSystem<CreateSnapshotSystem>();
		}

		protected override void OnUpdate()
		{
			var playerRelativeFromEntity = GetComponentDataFromEntity<Relative<PlayerDescription>>(true);
			var networkOwnerFromEntity   = GetComponentDataFromEntity<NetworkOwner>(true);
			var processFromEntity        = GetComponentDataFromEntity<FlowEngineProcess>();
			var settingsFromEntity       = GetComponentDataFromEntity<RhythmEngineSettings>(true);
			var stateFromEntity          = GetComponentDataFromEntity<RhythmEngineState>();
			var comboFromEntity          = GetComponentDataFromEntity<GameComboState>();

			var ghostMap = m_CreateSnapshotSystem.GhostToEntityMap;

			var tick = World.GetExistingSystem<ServerSimulationSystemGroup>().ServerTick;

			Entities
				.ForEach((in RhythmRpcClientRecover ev, in ReceiveRpcCommandRequestComponent receiveData) =>
				{
					if (!ghostMap.TryGetValue(ev.EngineGhostId, out var ghostEntity))
						return;
					if (!playerRelativeFromEntity.TryGet(ghostEntity, out var playerRelative)
					    && !networkOwnerFromEntity.TryGet(playerRelative.Target, out var networkOwner)
					    && networkOwner.Value != receiveData.SourceConnection)
						return;

					var process = processFromEntity[ghostEntity];
					var combo   = comboFromEntity[ghostEntity];
					var state   = stateFromEntity[ghostEntity];

					var flowBeat = process.GetFlowBeat(settingsFromEntity[ghostEntity].BeatInterval);
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

					comboFromEntity[ghostEntity] = combo;
					stateFromEntity[ghostEntity] = state;
				})
				.WithReadOnly(ghostMap)
				.WithReadOnly(processFromEntity)
				.WithReadOnly(settingsFromEntity)
				.WithReadOnly(playerRelativeFromEntity)
				.WithReadOnly(networkOwnerFromEntity)
				.WithNativeDisableParallelForRestriction(stateFromEntity)
				.WithNativeDisableParallelForRestriction(comboFromEntity)
				.Schedule();

			m_EndBarrier.CreateCommandBuffer().DestroyEntity(m_EventQuery);
			m_EndBarrier.AddJobHandleForProducer(Dependency);
		}
	}
}