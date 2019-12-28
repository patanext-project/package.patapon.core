using JetBrains.Annotations;
using package.stormiumteam.shared;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Definitions;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.RhythmEngine.Rpc;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Patapon.Mixed.Systems
{
	public class RhythmEngineCheckCommandValidity : JobGameBaseSystem
	{
		[BurstCompile]
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
		
		public static bool SameAsSequence(DynamicBuffer<RhythmCommandDefinitionSequence> commandSequence, DynamicBuffer<FlowPressure> currentCommand, bool predict)
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

		public static void GetCommand(DynamicBuffer<FlowPressure> currentCommand, NativeList<Entity>       commandsOutput, bool                                                      isPredicted,
		                              NativeArray<ArchetypeChunk> chunks,         ArchetypeChunkEntityType entityType,     ArchetypeChunkBufferType<RhythmCommandDefinitionSequence> commandSequenceType)
		{
			for (var chunk = 0; chunk != chunks.Length; chunk++)
			{
				var entityArray    = chunks[chunk].GetNativeArray(entityType);
				var containerArray = chunks[chunk].GetBufferAccessor(commandSequenceType);

				var count = chunks[chunk].Count;
				for (var ent = 0; ent != count; ent++)
				{
					var container = containerArray[ent].Reinterpret<RhythmCommandDefinitionSequence>();
					if (SameAsSequence(container, currentCommand, isPredicted))
					{
						commandsOutput.Add(entityArray[ent]);
						if (!isPredicted)
							return;
					}
				}
			}
		}

		private EntityQuery                                             m_AvailableCommandQuery;
		private OrderGroup.Simulation.SpawnEntities.CommandBufferSystem m_SpawnBarrier;

		private RpcQueue<RhythmRpcNewClientCommand> m_RpcQueue;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AvailableCommandQuery = GetEntityQuery(typeof(RhythmCommandDefinition), typeof(RhythmCommandDefinitionSequence));
			m_SpawnBarrier          = World.GetOrCreateSystem<OrderGroup.Simulation.SpawnEntities.CommandBufferSystem>();

			m_RpcQueue = World.GetOrCreateSystem<RpcSystem>().GetRpcQueue<RhythmRpcNewClientCommand>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (!HasSingleton<NetworkIdComponent>())
				return inputDeps;

			m_AvailableCommandQuery.AddDependency(inputDeps);

			var targetConnection       = GetSingletonEntity<NetworkIdComponent>();
			var outgoingDataFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>();

			var rpcQueue            = m_RpcQueue;
			var isServer            = IsServer;
			var availableCommands   = m_AvailableCommandQuery.CreateArchetypeChunkArray(Allocator.TempJob, out var queryHandle);
			var entityType          = GetArchetypeChunkEntityType();
			var commandSequenceType = GetArchetypeChunkBufferType<RhythmCommandDefinitionSequence>(true);
			var ecb                 = m_SpawnBarrier.CreateCommandBuffer().ToConcurrent();
			inputDeps =
				Entities
					.WithAll<FlowSimulateProcess>()
					.ForEach((int                                               nativeThreadIndex, ref RhythmEngineSettings settings, ref RhythmEngineState state,
					          ref FlowEngineProcess                             process,           ref RhythmCurrentCommand rhythmCurrentCommand,
					          ref DynamicBuffer<RhythmEngineCommandProgression> commandProgression) =>
					{
						if (state.IsPaused || process.Milliseconds < 0 || (!isServer && !state.IsNewPressure))
							return;

						if (isServer && settings.UseClientSimulation && !state.VerifyCommand)
							return;

						var cmdOutput = new NativeList<Entity>(1, Allocator.Temp);
						GetCommand(commandProgression.Reinterpret<FlowPressure>(), cmdOutput, false,
							availableCommands, entityType, commandSequenceType);

						rhythmCurrentCommand.HasPredictedCommands = false;
						if (cmdOutput.Length == 0)
						{
							GetCommand(commandProgression.Reinterpret<FlowPressure>(), cmdOutput, true,
								availableCommands, entityType, commandSequenceType);
							if (cmdOutput.Length > 0)
								rhythmCurrentCommand.HasPredictedCommands = true;

							return;
						}

						state.IsNewPressure = false;

						var targetBeat = process.GetFlowBeat(settings.BeatInterval) + 1;

						if (isServer)
						{
							var clientPressureBeat = FlowEngineProcess.CalculateFlowBeat(commandProgression[commandProgression.Length - 1].Data.Time, settings.BeatInterval) + 1;
							if (clientPressureBeat < targetBeat
							    && clientPressureBeat <= process.GetActivationBeat(settings.BeatInterval) + 1)
							{
								targetBeat = clientPressureBeat;
							}
						}

						rhythmCurrentCommand.ActiveAtTime  = targetBeat * settings.BeatInterval;
						rhythmCurrentCommand.Previous      = rhythmCurrentCommand.CommandTarget;
						rhythmCurrentCommand.CommandTarget = cmdOutput[0];

						state.VerifyCommand        = false;
						state.ApplyCommandNextBeat = true;

						if (!isServer && settings.UseClientSimulation)
						{
							var clientRequest = new UnsafeAllocationLength<RhythmEngineClientRequestedCommandProgression>(Allocator.Temp, commandProgression.Length);
							for (var com = 0; com != clientRequest.Length; com++)
							{
								clientRequest[com] = new RhythmEngineClientRequestedCommandProgression {Data = commandProgression[com].Data};
							}

							rpcQueue.Schedule(outgoingDataFromEntity[targetConnection], new RhythmRpcNewClientCommand {IsValid = true, ResultBuffer = clientRequest});

							clientRequest.Dispose();
						}

						var power = 0.0f;
						for (var i = 0; i != commandProgression.Length; i++)
						{
							// perfect
							if (commandProgression[i].Data.GetAbsoluteScore() <= 0.15f)
							{
								power += 1.0f;
							}
							else
							{
								power += 0.33f;
							}
						}

						rhythmCurrentCommand.Power = math.clamp((int) math.ceil(power * 100 / commandProgression.Length), 0, 100);

						commandProgression.Clear();
					})
					.WithNativeDisableParallelForRestriction(outgoingDataFromEntity)
					.WithDeallocateOnJobCompletion(availableCommands)
					.WithNativeDisableParallelForRestriction(availableCommands)
					.WithReadOnly(entityType)
					.WithReadOnly(commandSequenceType)
					.Schedule(JobHandle.CombineDependencies(inputDeps, queryHandle));

			m_SpawnBarrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}