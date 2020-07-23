﻿using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
 using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine.Structures;
 using Unity.Entities;

 namespace PataNext.Module.Simulation.Components.GamePlay.RhythmEngine
{
	public struct RhythmEnginePredictedCommandBuffer : IBufferElementData
	{
		public FlowPressure Value;

		public class Register : RegisterGameHostComponentBuffer<RhythmEnginePredictedCommandBuffer>
		{
		}
	}
}