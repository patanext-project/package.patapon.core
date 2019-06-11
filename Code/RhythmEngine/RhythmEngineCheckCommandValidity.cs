using JetBrains.Annotations;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UpdateAfter(typeof(RhythmEngineRemoveOldCommandPressureSystem))]
	[UsedImplicitly]
	public class RhythmEngineCheckCommandValidity : JobGameBaseSystem
	{
		[BurstCompile]
		[ExcludeComponent(typeof(NetworkStreamDisconnected))]
		private struct GetRpcTargetConnectionJob : IJobForEachWithEntity<NetworkStreamConnection>
		{
			public NativeArray<Entity> Target;

			[BurstDiscard]
			private void NonBurst_ThrowWarning()
			{
				Debug.LogWarning($"'{nameof(GetRpcTargetConnectionJob)}' already found a {nameof(Target)} = {Target[0]}");
			}

			public void Execute(Entity entity, int jobIndex, ref NetworkStreamConnection connection)
			{
				if (Target[0] != default)
				{
					NonBurst_ThrowWarning();
					return;
				}

				Target[0] = entity;
			}
		}

		[BurstCompile]
		[RequireComponentTag(typeof(FlowRhythmEngineSimulateTag))]
		private struct VerifyJob : IJobForEachWithEntity<RhythmEngineSettings, RhythmEngineState, FlowRhythmEngineProcess, FlowCurrentCommand>
		{
			[DeallocateOnJobCompletion, NativeDisableParallelForRestriction]
			public NativeArray<ArchetypeChunk> AvailableCommandChunks;

			[ReadOnly,DeallocateOnJobCompletion] public NativeArray<Entity> RpcTargetConnection;
			public            bool                IsServer;

			[ReadOnly] public ArchetypeChunkEntityType                               EntityType;
			[ReadOnly] public ArchetypeChunkBufferType<FlowCommandSequenceContainer> FlowCommandSequenceType;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<RhythmEngineCurrentCommand> CurrentCommandFromEntity;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingDataFromEntity;

			public RpcQueue<RhythmRpcNewClientCommand> RpcClientCommandQueue;

			public bool SameAsSequence(DynamicBuffer<FlowCommandSequence> commandSequence, DynamicBuffer<FlowRhythmPressureData> currentCommand)
			{
				if (commandSequence.Length != currentCommand.Length)
					return false;

				var offset = currentCommand[0].CorrectedBeat;
				for (var com = 0; com != commandSequence.Length; com++)
				{
					var range   = commandSequence[com].BeatRange;
					var comBeat = currentCommand[com].CorrectedBeat - offset;

					if (commandSequence[com].Key != currentCommand[com].KeyId)
						return false;

					if (!(range.start >= comBeat && comBeat <= range.end))
						return false;
				}

				return true;
			}

			public Entity GetCurrentCommand(DynamicBuffer<FlowRhythmPressureData> currentCommand)
			{
				for (var chunk = 0; chunk != AvailableCommandChunks.Length; chunk++)
				{
					var entityArray    = AvailableCommandChunks[chunk].GetNativeArray(EntityType);
					var containerArray = AvailableCommandChunks[chunk].GetBufferAccessor(FlowCommandSequenceType);

					var count = AvailableCommandChunks[chunk].Count;
					for (var ent = 0; ent != count; ent++)
					{
						var container = containerArray[ent].Reinterpret<FlowCommandSequence>();
						if (SameAsSequence(container, currentCommand))
						{
							return entityArray[ent];
						}
					}
				}

				return default;
			}

			public void Execute(Entity                                 entity,   int                    index,
			                    [ReadOnly] ref RhythmEngineSettings    settings, ref RhythmEngineState  state,
			                    ref            FlowRhythmEngineProcess process,  ref FlowCurrentCommand flowCurrentCommand)
			{
				if (state.IsPaused)
					return;

				if (IsServer && settings.UseClientSimulation && !state.ApplyCommandNextBeat)
					return;

				var currCommandArray = CurrentCommandFromEntity[entity];
				var result           = GetCurrentCommand(currCommandArray.Reinterpret<FlowRhythmPressureData>());

				if (result == default)
				{
					flowCurrentCommand.ActiveAtBeat  = -1;
					flowCurrentCommand.CustomEndBeat = -1;
					flowCurrentCommand.CommandTarget = default;

					state.ApplyCommandNextBeat = false;

					return;
				}

				flowCurrentCommand.ActiveAtBeat  = process.Beat + 1;
				flowCurrentCommand.CommandTarget = result;

				state.ApplyCommandNextBeat = false;

				if (!IsServer && settings.UseClientSimulation)
				{
					var clientRequest = new NativeArray<RhythmEngineClientRequestedCommand>(currCommandArray.Length, Allocator.Temp);
					for (var com = 0; com != clientRequest.Length; com++)
					{
						clientRequest[com] = new RhythmEngineClientRequestedCommand {Data = currCommandArray[com].Data};
					}

					RpcClientCommandQueue.Schedule(OutgoingDataFromEntity[RpcTargetConnection[0]], new RhythmRpcNewClientCommand {IsValid = true, ResultBuffer = clientRequest});
					
					clientRequest.Dispose();
				}
				
				currCommandArray.Clear();
			}
		}

		private EntityQuery m_AvailableCommandQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AvailableCommandQuery = GetEntityQuery(typeof(FlowCommandSequenceContainer));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_AvailableCommandQuery.AddDependency(inputDeps);

			var rpcTargetConnection = new NativeArray<Entity>(1, Allocator.TempJob);

			if (!IsServer)
			{
				inputDeps = new GetRpcTargetConnectionJob
				{
					Target = rpcTargetConnection
				}.ScheduleSingle(this, inputDeps);
			}

			inputDeps = new VerifyJob
			{
				IsServer            = IsServer,
				RpcTargetConnection = rpcTargetConnection,

				AvailableCommandChunks   = m_AvailableCommandQuery.CreateArchetypeChunkArray(Allocator.TempJob, out var queryHandle),
				EntityType               = GetArchetypeChunkEntityType(),
				FlowCommandSequenceType  = GetArchetypeChunkBufferType<FlowCommandSequenceContainer>(true),
				CurrentCommandFromEntity = GetBufferFromEntity<RhythmEngineCurrentCommand>(),

				OutgoingDataFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>(),
				RpcClientCommandQueue  = World.GetExistingSystem<RpcQueueSystem<RhythmRpcNewClientCommand>>().GetRpcQueue()
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, queryHandle));

			return inputDeps;
		}
	}
}