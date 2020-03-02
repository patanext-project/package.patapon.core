using System;
using Patapon.Mixed.RhythmEngine.Definitions;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace package.patapon.core
{
	public struct RhythmCommandId : IComponentData
	{
		public int Value;
	}

	public unsafe class RhythmCommandBuilder : AbsGameBaseSystem
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			EntityManager.CreateEntity(typeof(StockCommandId));
		}

		protected override void OnUpdate()
		{
		}

		public Entity GetOrCreate(NativeArray<RhythmCommandDefinitionSequence> sequence, bool dispose = true)
		{
			if (sequence[0].BeatRange.start != 0)
				throw new Exception("The first sequence should start at beat 0.");

			Entity finalEntity = default;

			Entities.WithAll<RhythmCommandEntityTag>().ForEach((Entity entity, DynamicBuffer<RhythmCommandDefinitionSequence> sequenceBuffer) =>
			{
				if (sequenceBuffer.Length != sequence.Length)
					return;

				if (UnsafeUtility.MemCmp(sequenceBuffer.GetUnsafePtr(), sequence.GetUnsafePtr(), sizeof(RhythmCommandDefinitionSequence) * sequenceBuffer.Length) == 0)
					finalEntity = entity;
			}).Run();

			if (finalEntity != default)
			{
				if (dispose) sequence.Dispose();

				return finalEntity;
			}

			finalEntity = EntityManager.CreateEntity(typeof(RhythmCommandEntityTag), typeof(RhythmCommandDefinitionSequence));

			EntityManager.GetBuffer<RhythmCommandDefinitionSequence>(finalEntity)
			             .CopyFrom(sequence);

			if (dispose) sequence.Dispose();

			var id = GetSingleton<StockCommandId>().LastId + 1;
			EntityManager.AddComponentData(finalEntity, new RhythmCommandId
			{
				Value = id
			});

			SetSingleton(new StockCommandId {LastId = id});

			return finalEntity;
		}

		public struct StockCommandId : IComponentData
		{
			public int LastId;
		}

		public struct RhythmCommandEntityTag : IComponentData
		{
		}
	}
}