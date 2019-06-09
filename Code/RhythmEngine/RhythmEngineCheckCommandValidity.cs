using JetBrains.Annotations;
using package.patapon.core;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(RhythmEngineGroup))]
	[UpdateAfter(typeof(RhythmEngineRemoveOldCommandPressureSystem))]
	[UsedImplicitly]
	public class RhythmEngineCheckCommandValidity : JobGameBaseSystem
	{
		[BurstCompile]
		private struct VerifyJob : IJobForEachWithEntity<RhythmEngineSettings, RhythmEngineState, FlowRhythmEngineProcess, FlowCurrentCommand>
		{
			[DeallocateOnJobCompletion, NativeDisableParallelForRestriction]
			public NativeArray<ArchetypeChunk> AvailableCommandChunks;

			[ReadOnly] public ArchetypeChunkEntityType                               EntityType;
			[ReadOnly] public ArchetypeChunkBufferType<FlowCommandSequenceContainer> FlowCommandSequenceType;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<RhythmEngineCurrentCommand> CurrentCommandFromEntity;

			public bool SameAsSequence(DynamicBuffer<FlowCommandSequence> commandSequence, DynamicBuffer<FlowRhythmPressureData> currentCommand)
			{
				if (commandSequence.Length != currentCommand.Length)
					return false;

				var offset = currentCommand[0].CorrectedBeat;
				for (var com = 0; com != commandSequence.Length; com++)
				{
					var range   = commandSequence[com].BeatRange;
					var comBeat = currentCommand[com].CorrectedBeat;

					if (!(range.start >= comBeat && comBeat <= range.end))
						return false;
				}

				return true;
			}

			public Entity GetCurrentCommand(DynamicBuffer<FlowRhythmPressureData> currentCommand)
			{
				for (var chunk = 0; chunk != AvailableCommandChunks.Length; chunk++)
				{
					var entityArray    = AvailableCommandChunks[chunk].GetNativeArray(EntityType);
					var containerArray = AvailableCommandChunks[chunk].GetBufferAccessor(FlowCommandSequenceType);

					var count = AvailableCommandChunks[chunk].Count;
					for (var ent = 0; ent != count; ent++)
					{
						var container = containerArray[ent].Reinterpret<FlowCommandSequence>();
						if (SameAsSequence(container, currentCommand))
						{
							return entityArray[ent];
						}
					}
				}

				return default;
			}

			public void Execute(Entity                                 entity,   int                    index,
			                    [ReadOnly] ref RhythmEngineSettings    settings, ref RhythmEngineState  state,
			                    ref            FlowRhythmEngineProcess process,  ref FlowCurrentCommand flowCurrentCommand)
			{
				if (state.IsPaused)
					return;

				if (settings.UseClientSimulation && !state.ApplyCommandNextBeat)
					return;

				var result = GetCurrentCommand(CurrentCommandFromEntity[entity].Reinterpret<FlowRhythmPressureData>());
				Debug.Log(result);

				flowCurrentCommand.IsActive      = 1;
				flowCurrentCommand.ActiveAtBeat  = process.Beat;
				flowCurrentCommand.CommandTarget = result;

				state.ApplyCommandNextBeat = false;
			}
		}

		private EntityQuery m_AvailableCommandQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AvailableCommandQuery = GetEntityQuery(typeof(FlowCommandSequenceContainer));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_AvailableCommandQuery.AddDependency(inputDeps);

			return new VerifyJob
			{
				AvailableCommandChunks   = m_AvailableCommandQuery.CreateArchetypeChunkArray(Allocator.TempJob, out var queryHandle),
				EntityType               = GetArchetypeChunkEntityType(),
				FlowCommandSequenceType  = GetArchetypeChunkBufferType<FlowCommandSequenceContainer>(true),
				CurrentCommandFromEntity = GetBufferFromEntity<RhythmEngineCurrentCommand>()
			}.Schedule(this, JobHandle.CombineDependencies(inputDeps, queryHandle));
		}
	}
}