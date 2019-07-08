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

			public bool SameAsSequence(DynamicBuffer<RhythmCommandSequence> commandSequence, DynamicBuffer<RhythmPressureData> currentCommand, bool predict)
			{
				if (currentCommand.Length <= 0)
					return false;

				var lastCommandBeat = currentCommand[currentCommand.Length - 1].RenderBeat;
				var commandLength   = commandSequence[commandSequence.Length - 1].BeatEnd - commandSequence[0].BeatRange.start;
				var startBeat       = lastCommandBeat - commandLength;

				// disable prediction for now (how should we do it? we should start by CurrentCommand instead of CommandSequence for the for-loop?)
				if (predict || currentCommand.Length < commandSequence.Length)
					return false;

				var comDiff = currentCommand.Length - commandSequence.Length;
				if (comDiff < 0)
					return false;

				for (var com = commandSequence.Length - 1; com >= 0; com--)
				{
					var range = commandSequence[com].BeatRange;
					range.start += startBeat;

					var comBeat = currentCommand[com + comDiff].RenderBeat;

					if (commandSequence[com].Key != currentCommand[com + comDiff].KeyId)
						return false;

					if (!(range.start <= comBeat && comBeat <= range.end))
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
						if (SameAsSequence(container, currentCommand, false))
						{
							return entityArray[ent];
						}
					}
				}

				return default;
			}

			public NativeList<Entity> GetPredictedCommands(DynamicBuffer<RhythmPressureData> currentCommand)
			{
				var list = new NativeList<Entity>(Allocator.Temp);
				for (var chunk = 0; chunk != AvailableCommandChunks.Length; chunk++)
				{
					var entityArray    = AvailableCommandChunks[chunk].GetNativeArray(EntityType);
					var containerArray = AvailableCommandChunks[chunk].GetBufferAccessor(FlowCommandSequenceType);

					var count = AvailableCommandChunks[chunk].Count;
					for (var ent = 0; ent != count; ent++)
					{
						var container = containerArray[ent].Reinterpret<RhythmCommandSequence>();
						if (SameAsSequence(container, currentCommand, true))
						{
							list.Add(entityArray[ent]);
						}
					}
				}

				return list;
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

				rhythmCurrentCommand.HasPredictedCommands = false;
				if (result == default)
				{
					var predictions = GetPredictedCommands(currCommandArray.Reinterpret<RhythmPressureData>());
					if (predictions.Length > 0)
						rhythmCurrentCommand.HasPredictedCommands = true;

					predictions.Dispose();

					return;
				}

				state.IsNewPressure = false;

				var targetBeat  = process.GetFlowBeat(settings.BeatInterval) + 1;
				
				if (IsServer)
				{
					var clientPressureBeat = RhythmEngineProcess.CalculateFlowBeat(currCommandArray[currCommandArray.Length - 1].Data.Time, settings.BeatInterval) + 1;
					if (clientPressureBeat < targetBeat
					    && clientPressureBeat <= process.GetActivationBeat(settings.BeatInterval) + 1)
					{
						targetBeat = clientPressureBeat;
					}
				}

				rhythmCurrentCommand.ActiveAtTime  = targetBeat * settings.BeatInterval;
				rhythmCurrentCommand.CommandTarget = result;
				
				state.VerifyCommand        = false;
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

				rhythmCurrentCommand.Power = math.clamp((int) math.ceil(power * 100 / currCommandArray.Length), 0, 100);

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