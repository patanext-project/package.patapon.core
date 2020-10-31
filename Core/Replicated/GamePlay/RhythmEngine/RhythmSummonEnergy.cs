using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using Unity.Entities;

namespace PataNext.Simulation.Mixed.Components.GamePlay.RhythmEngine
{
	public struct RhythmSummonEnergy : IComponentData
	{
		public int Value;

		public class Register : RegisterGameHostComponentData<RhythmSummonEnergy>
		{
		}
	}

	public struct RhythmSummonEnergyMax : IComponentData
	{
		public int MaxValue;

		public class Register : RegisterGameHostComponentData<RhythmSummonEnergyMax>
		{
		}
	}
}