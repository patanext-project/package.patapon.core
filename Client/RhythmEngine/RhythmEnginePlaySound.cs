using System;
using System.Collections.Generic;
using DefaultNamespace;
using Misc;
using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = Unity.Mathematics.Random;

namespace RhythmEngine
{
	[AlwaysUpdateSystem]
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class RhythmEnginePlaySound : GameBaseSystem
	{
		public float DelayTillNextBeat;
		public int PreviousBeat;
		public bool RequestSchedule;
		
		public bool IsNewPressure;

		private AsyncOperationModule m_AsyncOpModule;

		private AudioClip                                   m_AudioOnNewBeat;
		private AudioClip                                   m_AudioOnPerfect;
		private Dictionary<int, Dictionary<int, AudioClip>> m_AudioOnPressureDrum;
		private Dictionary<int, Dictionary<int, AudioClip>> m_AudioOnPressureVoice;

		private AudioSource m_AudioSourceOnNewBeat;
		private AudioSource m_AudioSourceOnNewPressureDrum;
		private AudioSource m_AudioSourceOnNewPressureVoice;
		private AudioSource m_AudioSourceOnPerfect;

		private EntityQuery m_EngineQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EngineQuery          = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));
			m_AudioOnPressureDrum  = new Dictionary<int, Dictionary<int, AudioClip>>();
			m_AudioOnPressureVoice = new Dictionary<int, Dictionary<int, AudioClip>>();

			void AddAsset(string path, Data data)
			{
				m_AsyncOpModule.Add(Addressables.LoadAssetAsync<AudioClip>($"core://Client/Sounds/Rhythm/{path}"), data);
			}

			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;

				return audioSource;
			}

			m_AudioSourceOnNewBeat          = CreateAudioSource("On New Beat", 0.7f);
			m_AudioSourceOnNewPressureDrum  = CreateAudioSource("On New Pressure -> Drum", 1);
			m_AudioSourceOnNewPressureVoice = CreateAudioSource("On New Pressure -> Voice", 1);
			m_AudioSourceOnPerfect = CreateAudioSource("On Perfect Pressure", 1);

			m_AudioSourceOnNewPressureDrum.priority = m_AudioSourceOnNewBeat.priority - 1;
			m_AudioSourceOnNewPressureVoice.priority = m_AudioSourceOnNewPressureDrum.priority - 1;
			m_AudioSourceOnPerfect.priority = m_AudioSourceOnNewPressureDrum.priority + 1;

			GetModule(out m_AsyncOpModule);

			AddAsset("Effects/on_new_beat.ogg", new Data {Type = DataType.Beat});
			AddAsset("Effects/perfect_1.wav", new Data {Type   = DataType.Perfect});
			for (var i = 0; i != 4; i++)
			{
				var key = i + 1;

				m_AudioOnPressureVoice[key] = new Dictionary<int, AudioClip>(3);
				m_AudioOnPressureDrum[key]  = new Dictionary<int, AudioClip>(3);

				for (var r = 0; r != 3; r++)
				{
					var rank = r;

					m_AudioOnPressureDrum[key][rank]  = null;
					m_AudioOnPressureVoice[key][rank] = null;

					AddAsset($"Drums/drum_{key}_{rank}.ogg", new Data {Type  = DataType.Pressure, Pressure = PressureType.Drum, PressureKey  = key, PressureRank = rank});
					AddAsset($"Drums/voice_{key}_{rank}.wav", new Data {Type = DataType.Pressure, Pressure = PressureType.Voice, PressureKey = key, PressureRank = rank});
				}
			}
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i < m_AsyncOpModule.Handles.Count; i++)
			{
				var (handle, data) = m_AsyncOpModule.Get<AudioClip, Data>(i);
				if (!handle.IsDone)
					continue;

				if (handle.Result != null)
					switch (data.Type)
					{
						case DataType.Beat:
							m_AudioOnNewBeat = handle.Result;
							break;
						case DataType.Pressure:
							// C#8 will bring a cleaner way to do all of these switches
							Dictionary<int, Dictionary<int, AudioClip>> dictionary;
							switch (data.Pressure)
							{
								case PressureType.Drum:
									dictionary = m_AudioOnPressureDrum;
									break;
								case PressureType.Voice:
									dictionary = m_AudioOnPressureVoice;
									break;
								default:
									throw new InvalidOperationException();
							}

							dictionary[data.PressureKey][data.PressureRank] = handle.Result;

							break;
						case DataType.Perfect:
							m_AudioOnPerfect = handle.Result;
							break;
					}

				m_AsyncOpModule.Handles.RemoveAtSwapBack(i);
				i--;
			}

			InitializeValues();

			if (RequestSchedule && DelayTillNextBeat > 0 && m_AudioOnNewBeat != null)
			{
				m_AudioSourceOnNewBeat.PlayScheduled(AudioSettings.dspTime + DelayTillNextBeat);
				m_AudioSourceOnNewBeat.clip = m_AudioOnNewBeat;
			}

			ClearValues();
		}

		private int m_LastFrame;

		private void InitializeValues()
		{
			var player = this.GetFirstSelfGamePlayer();
			if (player == default)
				return;

			Entity engine;
			if (this.TryGetCurrentCameraState(player, out var camState))
				engine = PlayerComponentFinder.GetComponentFromPlayer<RhythmEngineDescription>(EntityManager, m_EngineQuery, camState.Target, player);
			else
				engine = PlayerComponentFinder.FindPlayerComponent(m_EngineQuery, player);

			if (engine == default)
				return;

			var engineState = EntityManager.GetComponentData<RhythmEngineState>(engine);
			var process     = EntityManager.GetComponentData<FlowEngineProcess>(engine);
			var settings    = EntityManager.GetComponentData<RhythmEngineSettings>(engine);

			// don't do player sounds if it's paused or it didn't started yet
			if (engineState.IsPaused || process.GetFlowBeat(settings.BeatInterval) < 0)
				return;
			
			var activationBeat = process.GetActivationBeat(settings.BeatInterval);
			var targetBeat = process.GetFlowBeat(settings.BeatInterval);
			DelayTillNextBeat = (settings.BeatInterval - ((targetBeat + 1) * settings.BeatInterval - process.Milliseconds)) * 0.001f;
			if (DelayTillNextBeat > 0 && PreviousBeat != activationBeat)
			{
				PreviousBeat = activationBeat;
				RequestSchedule = true;
			}

			var playerOfEngine = EntityManager.GetComponentData<Relative<PlayerDescription>>(engine).Target;
			if (EntityManager.TryGetComponentData(playerOfEngine, out GamePlayerCommand playerCommand))
			{
				var localTick = World.GetExistingSystem<ClientSimulationSystemGroup>().ServerTick;

				//process.Milliseconds -= (int) (localTick - playerCommand.Base.Tick);

				var pressureKey   = -1;
				var rhythmActions = playerCommand.Base.GetRhythmActions();
				for (var i = 0; pressureKey < 0 && i != rhythmActions.Length; i++)
					if (rhythmActions[i].WasPressed)
						pressureKey = i;

				if (!EntityManager.HasComponent<GamePlayerLocalTag>(playerOfEngine) && m_LastFrame != playerCommand.Base.LastActionIndex
				    && playerCommand.Base.GetRhythmActions()[playerCommand.Base.LastActionIndex].IsActive)
				{
					m_LastFrame = playerCommand.Base.LastActionIndex;
					pressureKey = playerCommand.Base.LastActionIndex;
				}

				pressureKey++;
				if (pressureKey > 0)
				{
					IsNewPressure = true;


					var absRealScore = math.abs(FlowEngineProcess.GetScore(process.Milliseconds, settings.BeatInterval));
					var score        = 0;
					if (absRealScore <= FlowPressure.Perfect)
						score = 0;
					else
						score = 1;

					var gameCommandState = EntityManager.GetComponentData<GameCommandState>(engine);
					var currentCommand   = EntityManager.GetComponentData<RhythmCurrentCommand>(engine);
					var predictedCommand = EntityManager.GetComponentData<GamePredictedCommandState>(engine);

					var currFlowBeat = process.GetFlowBeat(settings.BeatInterval);
					var inputActive = gameCommandState.IsInputActive(process.Milliseconds, settings.BeatInterval)
					                  || predictedCommand.State.IsInputActive(process.Milliseconds, settings.BeatInterval);
					var commandIsRunning = gameCommandState.IsGamePlayActive(process.Milliseconds)
					                       || predictedCommand.State.IsGamePlayActive(process.Milliseconds);

					var shouldFail = commandIsRunning && !inputActive || engineState.IsRecovery(currFlowBeat) || absRealScore > FlowPressure.Error;
					if (shouldFail)
					{
						Debug.Log($"{commandIsRunning}, {inputActive}, {engineState.IsRecovery(currFlowBeat)}, {currFlowBeat} < {engineState.NextBeatRecovery} ({process.Milliseconds})");
						score = 2;
					}

					// do the perfect sound
					var isPerfect = !shouldFail && currentCommand.ActiveAtTime >= process.Milliseconds && currentCommand.Power >= 100;
					if (isPerfect) m_AudioSourceOnPerfect.PlayOneShot(m_AudioOnPerfect, 1.25f);

					m_AudioSourceOnNewPressureDrum.PlayOneShot(m_AudioOnPressureDrum[pressureKey][score]);

					if (GetSingleton<P4SoundRules.Data>().EnableDrumVoices) // voiceoverlay
					{
						var id         = score;
						if (id > 0) id = Mathf.Clamp(new Random((uint) Environment.TickCount).NextInt(-1, 3), 1, 2); // more chance to have a 1

						m_AudioSourceOnNewPressureVoice.PlayOneShot(m_AudioOnPressureVoice[pressureKey][id]);
					}
				}
			}
		}

		private void ClearValues()
		{
			RequestSchedule = false;
			DelayTillNextBeat = -1;
		}

		private enum DataType
		{
			Beat,
			Pressure,
			Perfect
		}

		private enum PressureType
		{
			Voice,
			Drum
		}

		private struct Data
		{
			public DataType     Type;
			public PressureType Pressure;
			public int          PressureKey;
			public int          PressureRank;
		}
	}
}