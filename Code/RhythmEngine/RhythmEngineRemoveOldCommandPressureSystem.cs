using JetBrains.Annotations;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UsedImplicitly]
	public class RhythmEngineRemoveOldCommandPressureSystem : JobGameBaseSystem
	{
		[RequireComponentTag(typeof(RhythmEngineSimulateTag))]
		private struct DeleteOldCommandJob : IJobChunk
		{
			public ArchetypeChunkComponentType<RhythmEngineProcess>     ProcessType;
			public ArchetypeChunkComponentType<RhythmEngineSettings>    SettingsType;
			public ArchetypeChunkComponentType<RhythmEngineState>       StateType;
			public ArchetypeChunkBufferType<RhythmEngineCurrentCommand> CurrCommandType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var predictedDataArray  = chunk.GetNativeArray(ProcessType);
				var settingsDataArray   = chunk.GetNativeArray(SettingsType);
				var stateDataArray      = chunk.GetNativeArray(StateType);
				var currCommandAccessor = chunk.GetBufferAccessor(CurrCommandType);

				var count = chunk.Count;

				for (var i = 0; i != count; i++)
				{
					var predictedData     = predictedDataArray[i];
					var settingsData      = settingsDataArray[i];
					var stateData         = stateDataArray[i];
					var currCommandBuffer = currCommandAccessor[i];

					var flowBeat = predictedData.GetFlowBeat(settingsData.BeatInterval);

					for (var j = 0; j != currCommandBuffer.Length; j++)
					{
						var currCommand = currCommandBuffer[j];
						if (flowBeat > currCommand.Data.RenderBeat + 1 + settingsData.MaxBeats
						    || stateData.IsRecovery(flowBeat))
						{
							Debug.Log($"Deleted (fb: {flowBeat}), {currCommand.Data.RenderBeat}");
							currCommandBuffer.RemoveAt(j);
							j--; // swap back method.
						}
					}
				}
			}
		}

		private EntityQuery m_EntityQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EntityQuery = GetEntityQuery(typeof(RhythmEngineSettings), typeof(RhythmEngineState), typeof(RhythmEngineCurrentCommand));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new DeleteOldCommandJob
			{
				ProcessType     = GetArchetypeChunkComponentType<RhythmEngineProcess>(),
				SettingsType    = GetArchetypeChunkComponentType<RhythmEngineSettings>(),
				StateType       = GetArchetypeChunkComponentType<RhythmEngineState>(),
				CurrCommandType = GetArchetypeChunkBufferType<RhythmEngineCurrentCommand>()
			}.Schedule(m_EntityQuery, inputDeps);

			return inputDeps;

		}
	}
}