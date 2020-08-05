﻿using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
 using GameHost.Simulation.Utility.Resource;
 using PataNext.Module.Simulation.Resources;
 using Unity.Entities;

 namespace PataNext.Module.Simulation.Components.GamePlay.RhythmEngine
{
	public struct RhythmEnginePredictedCommandBuffer : IBufferElementData
	{
		public GameResource<RhythmCommandResource> Value;

		public class Register : RegisterGameHostComponentBuffer<RhythmEnginePredictedCommandBuffer>
		{
		}
	}
}