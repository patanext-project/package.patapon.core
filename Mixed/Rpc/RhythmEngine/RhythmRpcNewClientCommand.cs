using System;
using package.stormiumteam.shared;
using Patapon.Mixed.RhythmEngine.Flow;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Patapon.Mixed.RhythmEngine.Rpc
{
	[BurstCompile]
	public unsafe struct RhythmRpcNewClientCommand : IRpcCommand
	{
		public bool                                                       IsValid;
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
}