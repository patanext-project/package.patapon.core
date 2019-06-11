using System;
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
	
	public unsafe class RhythmCommandBuilder : GameBaseSystem
	{
		public struct StockCommandId : IComponentData
		{
			public int LastId;
		}
		
		public struct RhythmCommandEntityTag : IComponentData
		{}

		protected override void OnCreate()
		{
			base.OnCreate();

			EntityManager.CreateEntity(typeof(StockCommandId));
		}

		protected override void OnUpdate()
		{
			
		}

		public Entity GetOrCreate(NativeArray<RhythmCommandSequence> sequence, bool dispose = true)
		{
			if (sequence[0].BeatRange.start != 0)
				throw new Exception("The first sequence should start at beat 0.");

			Entity finalEntity = default;

			Entities.WithAll<RhythmCommandEntityTag>().ForEach((Entity entity, DynamicBuffer<RhythmCommandSequenceContainer> sequenceBuffer) =>
			{
				if (sequenceBuffer.Length != sequence.Length)
					return;

				if (UnsafeUtility.MemCmp(sequenceBuffer.GetUnsafePtr(), sequence.GetUnsafePtr(), sizeof(RhythmCommandSequence) * sequenceBuffer.Length) == 0)
				{
					finalEntity = entity;
					return;
				}
			});

			if (finalEntity != default)
				return finalEntity;

			finalEntity = EntityManager.CreateEntity(typeof(RhythmCommandEntityTag), typeof(RhythmCommandSequenceContainer));

			var buffer = EntityManager.GetBuffer<RhythmCommandSequenceContainer>(finalEntity);

			buffer.Reinterpret<RhythmCommandSequence>().CopyFrom(sequence);
			if (dispose)
			{
				sequence.Dispose();
			}

			var id = GetSingleton<StockCommandId>().LastId + 1;
			EntityManager.AddComponentData(finalEntity, new RhythmCommandId
			{
				Value = id
			});

			SetSingleton(new StockCommandId {LastId = id});

			return finalEntity;
		}
	}
}