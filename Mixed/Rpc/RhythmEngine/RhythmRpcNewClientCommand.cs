using System;
using package.stormiumteam.shared;
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
using UnityEngine;

namespace Patapon.Mixed.RhythmEngine.Rpc
{
	[BurstCompile]
	public unsafe struct RhythmRpcNewClientCommand : IRpcCommand
	{
		public bool                                                                  IsValid;
		public UnsafeAllocationLength<RhythmEngineClientRequestedCommandProgression> ResultBuffer;

		[BurstDiscard]
		private void NonBurst_LogError()
		{
			Debug.LogError($"We tried to send an invalid '{nameof(ResultBuffer)}'!");
		}

		[BurstCompile]
		public static void Execute(ref RpcExecutor.Parameters parameters)
		{
			var ecb      = parameters.CommandBuffer;
			var jobIndex = parameters.JobIndex;

			var s = new RhythmRpcNewClientCommand();
			s.Deserialize(parameters.Reader, ref parameters.ReaderContext);

			var ent = ecb.CreateEntity(jobIndex);
			ecb.AddComponent(jobIndex, ent, new RhythmExecuteCommand
			{
				Connection = parameters.Connection
			});

			if (s.IsValid)
			{
				var b = ecb.AddBuffer<RhythmEngineClientRequestedCommandProgression>(jobIndex, ent);
				b.ResizeUninitialized(s.ResultBuffer.Length);
				for (var i = 0; i != s.ResultBuffer.Length; i++)
					b[i] = s.ResultBuffer[i];
			}

			s.ResultBuffer.Dispose();
		}

		public void Serialize(DataStreamWriter writer)
		{
			if (ResultBuffer.Data == null)
			{
				writer.Write((byte) 0); // validity
				NonBurst_LogError();
			}

			writer.Write((byte) 1);            // validity
			writer.Write(ResultBuffer.Length); // count
			for (var com = 0; com != ResultBuffer.Length; com++)
			{
				writer.Write(ResultBuffer[com].Data.Score);
				writer.Write(ResultBuffer[com].Data.KeyId);
				writer.Write(ResultBuffer[com].Data.Time);
				writer.Write(ResultBuffer[com].Data.RenderBeat);
			}
		}

		public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
		{
			IsValid = reader.ReadByte(ref ctx) == 1;
			if (!IsValid)
				return;

			var count = reader.ReadInt(ref ctx);
			ResultBuffer = new UnsafeAllocationLength<RhythmEngineClientRequestedCommandProgression>(Allocator.Persistent, count);
			for (var com = 0; com != count; com++)
			{
				var temp = default(FlowPressure);
				temp.Score      = reader.ReadFloat(ref ctx);
				temp.KeyId      = reader.ReadInt(ref ctx);
				temp.Time       = reader.ReadInt(ref ctx);
				temp.RenderBeat = reader.ReadInt(ref ctx);

				ResultBuffer[com] = new RhythmEngineClientRequestedCommandProgression {Data = temp};
			}
		}

		public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
		{
			return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(Execute);
		}

		public class RpcSystem : RpcCommandRequestSystem<RhythmRpcNewClientCommand>
		{
		}
	}


	public struct RhythmExecuteCommand : IComponentData
	{
		public Entity Connection;
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class RhythmExecuteCommandSystem : JobComponentSystem
	{
		private EndSimulationEntityCommandBufferSystem m_EndBarrier;
		private EntityQuery                            m_EngineQuery;
		private EntityQuery                            m_EventQuery;

		protected override void OnCreate()
		{
			m_EngineQuery = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));
			m_EventQuery  = GetEntityQuery(typeof(RhythmExecuteCommand));
			m_EndBarrier  = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override unsafe JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_EngineQuery.AddDependency(inputDeps);

			var engineChunks           = m_EngineQuery.CreateArchetypeChunkArray(Allocator.TempJob, out var queryHandle);
			var playerRelativeType     = GetArchetypeChunkComponentType<Relative<PlayerDescription>>(true);
			var networkOwnerFromEntity = GetComponentDataFromEntity<NetworkOwner>(true);
			var processType            = GetArchetypeChunkComponentType<FlowEngineProcess>();
			var settingsType           = GetArchetypeChunkComponentType<RhythmEngineSettings>(true);
			var stateType              = GetArchetypeChunkComponentType<RhythmEngineState>();

			var commandProgressionType          = GetArchetypeChunkBufferType<RhythmEngineCommandProgression>();
			var predictedCommandProgressionType = GetArchetypeChunkBufferType<RhythmEngineClientPredictedCommandProgression>();

			// <summary>
			// If true, players will be allowed to directly execute a command that may not be valid to the current one in the server
			// This should only be enabled if you can trust the players.
			// </summary>
			var allowCommandChange = true; // default value: false

			inputDeps =
				Entities
					.ForEach((Entity entity, int nativeThreadIndex, in RhythmExecuteCommand ev, in DynamicBuffer<RhythmEngineClientRequestedCommandProgression> requested) =>
					{
						for (var chunk = 0; chunk != engineChunks.Length; chunk++)
						{
							var count                            = engineChunks[chunk].Count;
							var playerRelativeArray              = engineChunks[chunk].GetNativeArray(playerRelativeType);
							var processArray                     = engineChunks[chunk].GetNativeArray(processType);
							var settingsArray                    = engineChunks[chunk].GetNativeArray(settingsType);
							var stateArray                       = engineChunks[chunk].GetNativeArray(stateType);
							var commandProgressionArray          = engineChunks[chunk].GetBufferAccessor(commandProgressionType);
							var predictedCommandProgressionArray = engineChunks[chunk].GetBufferAccessor(predictedCommandProgressionType);
							for (var ent = 0; ent != count; ent++)
							{
								if (!networkOwnerFromEntity.Exists(playerRelativeArray[ent].Target))
									continue;
								var targetConnectionEntity = networkOwnerFromEntity[playerRelativeArray[ent].Target].Value;
								if (targetConnectionEntity != ev.Connection)
									continue;

								ref var process = ref UnsafeUtilityEx.ArrayElementAsRef<FlowEngineProcess>(processArray.GetUnsafePtr(), ent);
								ref var state   = ref UnsafeUtilityEx.ArrayElementAsRef<RhythmEngineState>(stateArray.GetUnsafePtr(), ent);

								var commandProgression          = commandProgressionArray[ent].Reinterpret<FlowPressure>();
								var predictedCommandProgression = predictedCommandProgressionArray[ent].Reinterpret<FlowPressure>();

								Debug.Log($"{requested[0].Data.RenderBeat}(time={requested[0].Data.Time}), {requested[requested.Length - 1].Data.RenderBeat}(time={requested[requested.Length - 1].Data.Time})");
								if (requested[0].Data.RenderBeat != requested[requested.Length - 1].Data.RenderBeat - 3)
									Debug.Log("WHAT?");
								
								if (allowCommandChange)
								{
									commandProgression.CopyFrom(requested.Reinterpret<FlowPressure>());

									// it may be possible that client is delayed by one beat
									var lastCmdTime  = commandProgression[commandProgression.Length - 1].Time;
									var currFlowBeat = process.GetFlowBeat(settingsArray[ent].BeatInterval);
									var offset       = currFlowBeat - (commandProgression[0].RenderBeat + (settingsArray[ent].MaxBeats - 1));
								}
								else
								{
									throw new NotImplementedException("Prediction for commands is not yet implemented");
								}

								predictedCommandProgression.Clear();
								state.VerifyCommand = true;

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