using JetBrains.Annotations;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Patapon4TLB.Default
{
	[DisableAutoCreation]
	[UsedImplicitly]
	public class RhythmEngineCheckCurrentCommandValidity : GameBaseSystem
	{
		//[BurstCompile]
		private struct DeleteOldCommandJob : IJobChunk
		{
			public ArchetypeChunkComponentType<DefaultRhythmEngineData.Predicted> PredictedDataType;
			public ArchetypeChunkComponentType<DefaultRhythmEngineData.Settings>  SettingsDataType;
			public ArchetypeChunkBufferType<DefaultRhythmEngineCurrentCommand>    CurrCommandType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var predictedDataArray  = chunk.GetNativeArray(PredictedDataType);
				var settingsDataArray   = chunk.GetNativeArray(SettingsDataType);
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

		private EntityQuery m_ComponentGroup;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_ComponentGroup = GetEntityQuery(typeof(DefaultRhythmEngineData.Settings), typeof(DefaultRhythmEngineData.Predicted), typeof(DefaultRhythmEngineCurrentCommand));
		}

		protected override void OnUpdate()
		{
			SetDependency(new DeleteOldCommandJob
			{
				PredictedDataType = GetArchetypeChunkComponentType<DefaultRhythmEngineData.Predicted>(),
				SettingsDataType  = GetArchetypeChunkComponentType<DefaultRhythmEngineData.Settings>(),
				CurrCommandType   = GetArchetypeChunkBufferType<DefaultRhythmEngineCurrentCommand>()
			}.Schedule(m_ComponentGroup, GetDependency()));
		}
	}
}