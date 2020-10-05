using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.RhythmEngine
{
	/// <summary>
	/// Tag component
	/// </summary>
	public struct RhythmEngineIsPlaying : IComponentData
	{
		public class Register : RegisterGameHostComponentData<RhythmEngineIsPlaying>
		{
		}
	}
}