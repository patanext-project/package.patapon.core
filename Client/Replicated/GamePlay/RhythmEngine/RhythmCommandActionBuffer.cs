using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine.Structures;
using Unity.Entities;

namespace PataNext.Module.Simulation.Game.RhythmEngine
{
	public readonly struct RhythmCommandActionBuffer : IBufferElementData
	{
		public readonly RhythmCommandAction Value;

		public RhythmCommandActionBuffer(RhythmCommandAction value)
		{
			Value = value;
		}

		public class Register : RegisterGameHostComponentBuffer<RhythmCommandActionBuffer>
		{
		}
	}
}