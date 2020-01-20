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
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	public class RhythmEngineCheckCommandValidity : JobGameBaseSystem
	{
		private EntityQuery m_AvailableCommandQuery;

		private RpcQueue<RhythmRpcNewClientCommand>                     m_RpcQueue;
		private OrderGroup.Simulation.SpawnEntities.CommandBufferSystem m_SpawnBarrier;

		public static bool SameAsSequence(DynamicBuffer<RhythmCommandDefinitionSequence> commandSequence, DynamicBuffer<FlowPressure> currentCommand)
		{
			if (currentCommand.Length <= 0)
				return false;

			var lastCommandBeat = currentCommand[currentCommand.Length - 1].RenderBeat;
			var commandLength   = commandSequence[commandSequence.Length - 1].BeatEnd - commandSequence[0].BeatRange.start;
			var startBeat       = lastCommandBeat - commandLength;

			// disable prediction for now (how should we do it? we should start by CurrentCommand instead of CommandSequence for the for-loop?)
			if (currentCommand.Length < commandSequence.Length)
				return false;

			var comDiff = currentCommand.Length - commandSequence.Length;
			if (comDiff < 0)
				return false;

			if (!CanBePredicted(commandSequence, currentCommand))
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

		public static bool CanBePredicted(DynamicBuffer<RhythmCommandDefinitionSequence> commandSequence, DynamicBuffer<FlowPressure> currentCommand)
		{
			if (currentCommand.Length == 0)
				return true; // an empty command is valid

			var firstBeat = currentCommand[0].RenderBeat;
			for (int seq = 0, curr = 0; curr < currentCommand.Length; curr++)
			{
				if (!commandSequence[seq].ContainsInRange(currentCommand[curr].RenderBeat - firstBeat))
				{
					return false;
				}

				if (commandSequence[seq].Key != currentCommand[curr].KeyId)
					return false;

				if (seq > 0
				    && commandSequence[seq].MaxTimeDifference > 0
				    && (currentCommand[seq].Time - currentCommand[seq - 1].Time) * 0.001f >= commandSequence[seq].MaxTimeDifference)
				{
					return false;
				}

				seq++;
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
					if (!isPredicted && SameAsSequence(container, currentCommand))
					{
						commandsOutput.Add(entityArray[ent]);
						return;
					}

					if (isPredicted && CanBePredicted(container, currentCommand)) commandsOutput.Add(entityArray[ent]);
				}
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AvailableCommandQuery = GetEntityQuery(typeof(RhythmCommandDefinition), typeof(RhythmCommandDefinitionSequence));
			m_SpawnBarrier          = World.GetOrCreateSystem<OrderGroup.Simulation.SpawnEntities.CommandBufferSystem>();

			m_RpcQueue = World.GetOrCreateSystem<RpcSystem>().GetRpcQueue<RhythmRpcNewClientCommand>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (!IsServer && !HasSingleton<NetworkIdComponent>())
				return inputDeps;

			m_AvailableCommandQuery.AddDependency(inputDeps);

			Entity targetConnection = default;
			if (!IsServer)
				targetConnection       = GetSingletonEntity<NetworkIdComponent>();
			
			var outgoingDataFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>();

			//var commandDefinition = GetComponentDataFromEntity<RhythmCommandDefinition>(true); // debug only

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
						{
							if (state.IsPaused || process.Milliseconds < 0)
							{
								rhythmCurrentCommand = new RhythmCurrentCommand {CustomEndTime = -1, ActiveAtTime = -1};
							}
							return;
						}

						if (isServer && settings.UseClientSimulation && !state.VerifyCommand)
						{
							return;
						}

						var cmdOutput = new NativeList<Entity>(1, Allocator.Temp);
						GetCommand(commandProgression.Reinterpret<FlowPressure>(), cmdOutput, false,
							availableCommands, entityType, commandSequenceType);
						
						state.IsNewPressure = false;

						rhythmCurrentCommand.HasPredictedCommands = cmdOutput.Length == 1;
						if (cmdOutput.Length == 0)
						{
							GetCommand(commandProgression.Reinterpret<FlowPressure>(), cmdOutput, true,
								availableCommands, entityType, commandSequenceType);
							if (cmdOutput.Length > 0)
								rhythmCurrentCommand.HasPredictedCommands = true;

							/*if (!isServer) // debug only
							{
								var str = "";
								for (var index = 0; index < cmdOutput.Length; index++)
								{
									var cmd = cmdOutput[index];
									str += commandDefinition[cmd].Identifier.ToString() + ", ";
								}

								Debug.Log($"commands = {str}");
							}*/

							return;
						}

						// this is so laggy clients don't have a weird things when their command has been on another beat on the server
						var targetBeat = commandProgression[commandProgression.Length - 1].Data.RenderBeat + 1;

						if (isServer)
						{
							var clientPressureBeat = FlowEngineProcess.CalculateFlowBeat(commandProgression[commandProgression.Length - 1].Data.Time, settings.BeatInterval) + 1;
							if (clientPressureBeat < targetBeat
							    && clientPressureBeat <= process.GetActivationBeat(settings.BeatInterval) + 1)
								targetBeat = clientPressureBeat;
						}

						rhythmCurrentCommand.ActiveAtTime  = targetBeat * settings.BeatInterval;
						rhythmCurrentCommand.Previous      = rhythmCurrentCommand.CommandTarget;
						rhythmCurrentCommand.CommandTarget = cmdOutput[0];

						state.VerifyCommand        = false;
						state.ApplyCommandNextBeat = true;

						if (!isServer && settings.UseClientSimulation)
						{
							var clientRequest                                                        = new UnsafeAllocationLength<RhythmEngineClientRequestedCommandProgression>(Allocator.Temp, commandProgression.Length);
							for (var com = 0; com != clientRequest.Length; com++) clientRequest[com] = new RhythmEngineClientRequestedCommandProgression {Data = commandProgression[com].Data};

							rpcQueue.Schedule(outgoingDataFromEntity[targetConnection], new RhythmRpcNewClientCommand {IsValid = true, ResultBuffer = clientRequest});

							clientRequest.Dispose();
						}

						var power = 0.0f;
						for (var i = 0; i != commandProgression.Length; i++)
							// perfect
							if (commandProgression[i].Data.GetAbsoluteScore() <= FlowPressure.Perfect)
								power += 1.0f;
							else
								power += 0.33f;

						rhythmCurrentCommand.Power = math.clamp((int) math.ceil(power * 100 / commandProgression.Length), 0, 100);

						commandProgression.Clear();
					})
					.WithNativeDisableParallelForRestriction(outgoingDataFromEntity)
					.WithDeallocateOnJobCompletion(availableCommands)
					.WithNativeDisableParallelForRestriction(availableCommands)
					.WithReadOnly(entityType)
					.WithReadOnly(commandSequenceType)
					//.WithReadOnly(commandDefinition) // debug only
					.Schedule(JobHandle.CombineDependencies(inputDeps, queryHandle));

			m_SpawnBarrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}

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
	}
}