using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine.Structures;
using PataNext.Module.Simulation.Components.Units;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.RhythmEngine
{
	public struct RhythmEngineLocalCommandBuffer : IBufferElementData
	{
		public FlowPressure Value;

		public class Register : RegisterGameHostComponentBuffer<RhythmEngineLocalCommandBuffer>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultBufferDeserializer<RhythmEngineLocalCommandBuffer>();
		}
	}
}