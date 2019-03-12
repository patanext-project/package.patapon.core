using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace package.patapon.core
{
	public unsafe class FlowCommandBuilder : GameBaseSystem
	{
		public struct FlowCommandEntityTag : IComponentData
		{}
		
		protected override void OnUpdate()
		{
			
		}

		public Entity GetOrCreate(NativeArray<FlowCommandSequence> sequence, bool dispose = true)
		{
			Entity finalEntity = default;
			
			Entities.WithAll<FlowCommandEntityTag, FlowCommandSequenceContainer>().ForEach((Entity entity, DynamicBuffer<FlowCommandSequenceContainer> sequenceBuffer) =>
			{
				if (sequenceBuffer.Length != sequence.Length)
					return;

				if (UnsafeUtility.MemCmp(sequenceBuffer.GetUnsafePtr(), sequence.GetUnsafePtr(), sizeof(FlowCommandSequence) * sequenceBuffer.Length) != 0)
				{
					finalEntity = entity;
					return;
				}
			});

			if (finalEntity != default)
				return finalEntity;

			finalEntity = EntityManager.CreateEntity(typeof(FlowCommandEntityTag), typeof(FlowCommandSequenceContainer));

			var buffer = EntityManager.GetBuffer<FlowCommandSequenceContainer>(finalEntity);
			
			buffer.Reinterpret<FlowCommandSequence>().CopyFrom(sequence);
			if (dispose) sequence.Dispose();
			
			return finalEntity;
		}
	}
}