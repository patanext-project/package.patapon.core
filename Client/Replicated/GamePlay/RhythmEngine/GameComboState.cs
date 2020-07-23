﻿using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
 using Unity.Entities;

 namespace PataNext.Module.Simulation.Components.GamePlay.RhythmEngine
{
	public struct GameComboState : IComponentData
	{
		public class Register : RegisterGameHostComponentData<GameComboState>
		{
		}
	}
}