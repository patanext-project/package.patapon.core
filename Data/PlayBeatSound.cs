using System.Collections.Generic;
using package.patapon.core;
using Unity.Entities;
using Unity.Mathematics;
using Revolution.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace Patapon4TLB.Default.Test
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(RhythmEngineClientInputSystem))]
	public class PlayBeatSound : ComponentSystem
	{
		private int  m_PreviousActivationBeat;
		private bool m_Play;

		public bool VoiceOverlay;

		private void ForEachEngine(Entity e, ref RhythmEngineProcess process, ref RhythmEngineSettings settings)
		{
			var currActivationBeat = process.GetActivationBeat(settings.BeatInterval);
			if (m_PreviousActivationBeat == currActivationBeat)
				return;

			m_PreviousActivationBeat = currActivationBeat;
			m_Play                   = true;
		}

		private void ForEachPressureEvent(Entity entity, ref PressureEvent pressureEvent)
		{
			if (!EntityManager.HasComponent(pressureEvent.Engine, typeof(RhythmEngineSimulateTag)))
				return;

			var absRealScore = math.abs(pressureEvent.Score);
			var score        = 0;
			if (absRealScore <= 0.15f)
				score = 0;
			else
				score = 1;

			var gameCommandState = EntityManager.GetComponentData<GameCommandState>(pressureEvent.Engine);
			var currentCommand   = EntityManager.GetComponentData<RhythmCurrentCommand>(pressureEvent.Engine);
			var predictedCommand = EntityManager.GetComponentData<GamePredictedCommandState>(pressureEvent.Engine);
			var process          = EntityManager.GetComponentData<RhythmEngineProcess>(pressureEvent.Engine);
			var state            = EntityManager.GetComponentData<RhythmEngineState>(pressureEvent.Engine);
			var settings         = EntityManager.GetComponentData<RhythmEngineSettings>(pressureEvent.Engine);

			var currFlowBeat = process.GetFlowBeat(settings.BeatInterval);
			var inputActive = gameCommandState.IsInputActive(process.Milliseconds, settings.BeatInterval)
			                  || predictedCommand.State.IsInputActive(process.Milliseconds, settings.BeatInterval);
			var commandIsRunning = gameCommandState.IsGamePlayActive(process.Milliseconds)
			                       || predictedCommand.State.IsGamePlayActive(process.Milliseconds);

			var shouldFail = (commandIsRunning && !inputActive) || state.IsRecovery(currFlowBeat) || absRealScore > RhythmPressureData.Error;
			if (shouldFail)
			{
				score = 2;
			}

			// do the perfect sound
			var isPerfect = !shouldFail && currentCommand.ActiveAtTime >= process.Milliseconds && currentCommand.Power >= 100;
			if (isPerfect)
			{
				m_AudioSourceOnNewPressureDrum.PlayOneShot(m_AudioOnPerfect, 1.25f);
			}

			m_AudioSourceOnNewPressureDrum.PlayOneShot(m_AudioOnPressureDrum[pressureEvent.Key][score]);

			if (VoiceOverlay)
			{
				var id = score;
				if (id > 0)
				{
					id = Mathf.Clamp(Random.Range(-1, 3), 1, 2); // more chance to have a 1
				}

				m_AudioSourceOnNewPressureVoice.PlayOneShot(m_AudioOnPressureVoice[pressureEvent.Key][id]);
			}
		}

		private EntityQueryBuilder.F_EDD<RhythmEngineProcess, RhythmEngineSettings> m_EngineDelegate;
		private EntityQueryBuilder.F_ED<PressureEvent>                              m_PressureEventDelegate;

		private AudioSource m_AudioSourceOnNewBeat;
		private AudioSource m_AudioSourceOnNewPressureDrum;
		private AudioSource m_AudioSourceOnNewPressureVoice;

		private AudioClip                                   m_AudioOnNewBeat;
		private AudioClip                                   m_AudioOnPerfect;
		private Dictionary<int, Dictionary<int, AudioClip>> m_AudioOnPressureDrum;
		private Dictionary<int, Dictionary<int, AudioClip>> m_AudioOnPressureVoice;

		protected override void OnCreate()
		{
			VoiceOverlay = true;

			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;

				return audioSource;
			}

			base.OnCreate();

			if (!Application.isPlaying)
				return;

			Addressables.InitializationOperation.Completed += op => { OnLoadAssets(); };

			m_EngineDelegate        = ForEachEngine;
			m_PressureEventDelegate = ForEachPressureEvent;

			m_AudioSourceOnNewBeat          = CreateAudioSource("On New Beat", 1);
			m_AudioSourceOnNewPressureDrum  = CreateAudioSource("On New Pressure -> Drum", 1);
			m_AudioSourceOnNewPressureVoice = CreateAudioSource("On New Pressure -> Voice", 1);

			m_AudioSourceOnNewPressureVoice.priority = m_AudioSourceOnNewPressureDrum.priority - 1;
		}

		protected void OnLoadAssets()
		{
			Addressables.LoadAssetAsync<AudioClip>("int:RhythmEngine/Sounds/on_new_beat.ogg")
			            .Completed += op => m_AudioOnNewBeat = op.Result;

			Addressables.LoadAssetAsync<AudioClip>("int:RhythmEngine/Sounds/perfect_1.wav")
			            .Completed += op => m_AudioOnPerfect = op.Result;

			m_AudioOnPressureDrum  = new Dictionary<int, Dictionary<int, AudioClip>>(12);
			m_AudioOnPressureVoice = new Dictionary<int, Dictionary<int, AudioClip>>(12);

			for (int i = 0; i != 4; i++)
			{
				var key = i + 1;

				m_AudioOnPressureVoice[key] = new Dictionary<int, AudioClip>(3);
				m_AudioOnPressureDrum[key]  = new Dictionary<int, AudioClip>(3);

				for (int r = 0; r != 3; r++)
				{
					var rank = r;

					m_AudioOnPressureDrum[key][rank]  = null;
					m_AudioOnPressureVoice[key][rank] = null;

					Addressables.LoadAssetAsync<AudioClip>($"int:RhythmEngine/Sounds/drum_{key}_{rank}.ogg").Completed += op =>
					{
						Debug.Assert(op.IsValid(), "op.IsValid");

						m_AudioOnPressureDrum[key][rank] = op.Result;
					};

					Addressables.LoadAssetAsync<AudioClip>($"int:RhythmEngine/Sounds/voice_{key}_{rank}.wav").Completed += op =>
					{
						Debug.Assert(op.IsValid(), "op.IsValid");

						m_AudioOnPressureVoice[key][rank] = op.Result;
					};
				}
			}
		}

		protected override void OnUpdate()
		{
			if (!Application.isPlaying)
				return;

			m_Play = false;

			Entities.WithAll<RhythmEngineSimulateTag>().ForEach(m_EngineDelegate);

			if (m_Play)
			{
				m_AudioSourceOnNewBeat.PlayOneShot(m_AudioOnNewBeat);
			}

			Entities.ForEach(m_PressureEventDelegate);
		}
	}
}