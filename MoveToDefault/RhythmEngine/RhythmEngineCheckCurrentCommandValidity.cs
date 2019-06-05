using JetBrains.Annotations;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UsedImplicitly]
	public class RhythmEngineCheckCurrentCommandValidity : JobGameBaseSystem
	{
		//[BurstCompile]
		private struct DeleteOldCommandJob : IJobChunk
		{
			public ArchetypeChunkComponentType<DefaultRhythmEngineState>       StateType;
			public ArchetypeChunkComponentType<DefaultRhythmEngineSettings>    SettingsType;
			public ArchetypeChunkBufferType<DefaultRhythmEngineCurrentCommand> CurrCommandType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var predictedDataArray  = chunk.GetNativeArray(StateType);
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

			m_EntityQuery = GetEntityQuery(typeof(DefaultRhythmEngineSettings), typeof(DefaultRhythmEngineState), typeof(DefaultRhythmEngineCurrentCommand));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new DeleteOldCommandJob
			{
				StateType = GetArchetypeChunkComponentType<DefaultRhythmEngineState>(),
				SettingsType  = GetArchetypeChunkComponentType<DefaultRhythmEngineSettings>(),
				CurrCommandType   = GetArchetypeChunkBufferType<DefaultRhythmEngineCurrentCommand>()
			}.Schedule(m_EntityQuery, inputDeps);

			return inputDeps;
		}
	}
}