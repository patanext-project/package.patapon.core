using System;
using GameHost.ShareSimuWorldFeature;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine.Structures;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components.GamePlay.RhythmEngine
{
	public struct RhythmEngineLocalState : IRhythmEngineState, IComponentData
	{
		public FlowPressure LastPressure           { get; set; }
		public int          RecoveryActivationBeat { get; set; }
		public TimeSpan     Elapsed                { get; set; }
		public TimeSpan     PreviousStartTime;

		public bool CanRunCommands => Elapsed > TimeSpan.Zero;

		public int  CurrentBeat;
		public uint NewBeatTick;

		public bool IsRecovery(int activationBeat)
		{
			return RecoveryActivationBeat > activationBeat;
		}

		public class Register : RegisterGameHostComponentData<RhythmEngineLocalState>
		{
			protected override ICustomComponentDeserializer CustomDeserializer => new DefaultSingleDeserializer<RhythmEngineLocalState>();
		}
	}
}