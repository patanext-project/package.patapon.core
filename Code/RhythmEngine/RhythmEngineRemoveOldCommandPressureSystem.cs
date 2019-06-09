using JetBrains.Annotations;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UsedImplicitly]
	public class RhythmEngineRemoveOldCommandPressureSystem : JobGameBaseSystem
	{
		//[BurstCompile]
		private struct DeleteOldCommandJob : IJobChunk
		{
			public ArchetypeChunkComponentType<FlowRhythmEngineProcess> ProcessType;
			public ArchetypeChunkComponentType<RhythmEngineSettings>    SettingsType;
			public ArchetypeChunkBufferType<RhythmEngineCurrentCommand> CurrCommandType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var predictedDataArray  = chunk.GetNativeArray(ProcessType);
				var settingsDataArray   = chunk.GetNativeArray(SettingsType);
				var currCommandAccessor = chunk.GetBufferAccessor(CurrCommandType);

				var count = chunk.Count;

				for (var i = 0; i != count; i++)
				{
					var predictedData     = predictedDataArray[i];
					var settingsData      = settingsDataArray[i];
					var currCommandBuffer = currCommandAccessor[i];

					for (var j = 0; j != currCommandBuffer.Length; j++)
					{
						var currCommand = currCommandBuffer[j];
						if (predictedData.Beat + settingsData.MaxBeats < currCommand.Data.CorrectedBeat)
						{
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
				ProcessType     = GetArchetypeChunkComponentType<FlowRhythmEngineProcess>(),
				SettingsType    = GetArchetypeChunkComponentType<RhythmEngineSettings>(),
				CurrCommandType = GetArchetypeChunkBufferType<RhythmEngineCurrentCommand>()
			}.Schedule(m_EntityQuery, inputDeps);

			return inputDeps;
		}
	}
}