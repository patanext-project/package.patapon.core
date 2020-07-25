﻿using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
 using Unity.Entities;

 namespace PataNext.Module.Simulation.Components.GamePlay.RhythmEngine
 {
	 public struct GameCombo : IComponentData
	 {
		 public struct Settings : IComponentData
		 {
			 public int   MaxComboToReachFever;
			 public float RequiredScoreStart;
			 public float RequiredScoreStep;

			 public bool CanEnterFever(int combo, float score)
			 {
				 return combo > MaxComboToReachFever
				        || (RequiredScoreStart - combo * RequiredScoreStep) < score;
			 }

			 public class Register : RegisterGameHostComponentData<GameCombo.Settings>
			 {
			 }
		 }

		 public struct State : IComponentData
		 {
			 /// <summary>
			 /// Combo count
			 /// </summary>
			 public int Count;

			 /// <summary>
			 /// Combo score
			 /// </summary>
			 public float Score;

			 public class Register : RegisterGameHostComponentData<GameCombo.State>
			 {
			 }
		 }
	 }
 }