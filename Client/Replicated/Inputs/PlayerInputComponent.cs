using System;
using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.Utility.InterTick;
using package.stormiumteam.shared;
using Unity.Entities;

namespace PataNext.Module.Simulation.Components
{
	public enum AbilitySelection
	{
		Horizontal = 0,
		Top        = 1,
		Bottom     = 2
	}

	public unsafe struct PlayerInputComponent : IComponentData
	{
		public struct RhythmAction
		{
			public InterFramePressAction InterFrame;
			public bool                  IsActive;
		}

		public RhythmAction action0;
		public RhythmAction action1;
		public RhythmAction action2;
		public RhythmAction action3;

		public Span<RhythmAction> Actions
		{
			get
			{
				fixed (RhythmAction* fixedPtr = &action0)
					return new Span<RhythmAction>(fixedPtr, 4);
			}
		}

		public AbilitySelection Ability;
		public float            Panning;

		public class Register : RegisterGameHostComponentData<PlayerInputComponent>
		{
		}
	}
}