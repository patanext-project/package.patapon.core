using JetBrains.Annotations;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UpdateAfter(typeof(RhythmEngineRemoveOldCommandPressureSystem))]
	[UpdateAfter(typeof(RhythmEngineServerSimulateSystem))]
	[UpdateAfter(typeof(RhythmEngineClientSimulateLocalSystem))]
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
		[RequireComponentTag(typeof(RhythmEngineSimulateTag))]
		private struct VerifyJob : IJobForEachWithEntity<RhythmEngineSettings, RhythmEngineState, RhythmEngineProcess, RhythmCurrentCommand>
		{
			[DeallocateOnJobCompletion, NativeDisableParallelForRestriction]
			public NativeArray<ArchetypeChunk> AvailableCommandChunks;

			[ReadOnly, DeallocateOnJobCompletion] public NativeArray<Entity> RpcTargetConnection;
			public                                       bool                IsServer;

			[ReadOnly] public ArchetypeChunkEntityType                                 EntityType;
			[ReadOnly] public ArchetypeChunkBufferType<RhythmCommandSequenceContainer> FlowCommandSequenceType;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<RhythmEngineCurrentCommand> CurrentCommandFromEntity;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> OutgoingDataFromEntity;

			public RpcQueue<RhythmRpcNewClientCommand> RpcClientCommandQueue;

			public bool SameAsSequence(DynamicBuffer<RhythmCommandSequence> commandSequence, DynamicBuffer<RhythmPressureData> currentCommand)
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

			public Entity GetCurrentCommand(DynamicBuffer<RhythmPressureData> currentCommand)
			{
				for (var chunk = 0; chunk != AvailableCommandChunks.Length; chunk++)
				{
					var entityArray    = AvailableCommandChunks[chunk].GetNativeArray(EntityType);
					var containerArray = AvailableCommandChunks[chunk].GetBufferAccessor(FlowCommandSequenceType);

					var count = AvailableCommandChunks[chunk].Count;
					for (var ent = 0; ent != count; ent++)
					{
						var container = containerArray[ent].Reinterpret<RhythmCommandSequence>();
						if (SameAsSequence(container, currentCommand))
						{
							return entityArray[ent];
						}
					}
				}

				return default;
			}

			public void Execute(Entity                              entity,   int                      index,
			                    [ReadOnly] ref RhythmEngineSettings settings, ref RhythmEngineState    state,
			                    ref            RhythmEngineProcess  process,  ref RhythmCurrentCommand rhythmCurrentCommand)
			{
				if (state.IsPaused || (!IsServer && !state.IsNewPressure))
					return;

				if (IsServer && settings.UseClientSimulation && !state.VerifyCommand)
					return;
				
				var currCommandArray = CurrentCommandFromEntity[entity];
				var result           = GetCurrentCommand(currCommandArray.Reinterpret<RhythmPressureData>());

				if (result == default)
				{
					return;
				}

				state.IsNewPressure = false;


				var lastCommand = currCommandArray[currCommandArray.Length - 1].Data;
				var targetBeat  = lastCommand.CorrectedBeat + 1;

				rhythmCurrentCommand.ActiveAtBeat  = targetBeat;
				rhythmCurrentCommand.CommandTarget = result;

				state.VerifyCommand = false;
				state.ApplyCommandNextBeat = true;

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

				var power = 0.0f;
				for (var i = 0; i != currCommandArray.Length; i++)
				{
					// perfect
					if (currCommandArray[i].Data.GetAbsoluteScore() <= 0.15f)
					{
						power += 1.0f;
					}
					else
					{
						power += 0.33f;
					}
				}

				rhythmCurrentCommand.Power = math.clamp((int)math.ceil(power * 100 / currCommandArray.Length), 0, 100);
				
				currCommandArray.Clear();
			}
		}

		private EntityQuery m_AvailableCommandQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AvailableCommandQuery = GetEntityQuery(typeof(RhythmCommandSequenceContainer));
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
				FlowCommandSequenceType  = GetArchetypeChunkBufferType<RhythmCommandSequenceContainer>(true),
				CurrentCommandFromEntity = GetBufferFromEntity<RhythmEngineCurrentCommand>(),

				OutgoingDataFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>(),
				RpcClientCommandQueue  = World.GetExistingSystem<RpcQueueSystem<RhythmRpcNewClientCommand>>().GetRpcQueue()
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, queryHandle));

			return inputDeps;
		}
	}
}