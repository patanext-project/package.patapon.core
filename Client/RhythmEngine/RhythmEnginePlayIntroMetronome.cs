using System;
using System.Collections.Generic;
using Misc;
using Misc.Extensions;
using package.patapon.core.Animation.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RhythmEngine
{
	[AlwaysUpdateSystem]
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class RhythmEnginePlayIntroMetronome : GameBaseSystem
	{
		public bool IsNewBeat;
		public int BeatTarget;
		
		private AsyncOperationModule m_AsyncOpModule;

		private Dictionary<int, AudioClip> m_AudioOnNewBeat;

		private AudioSource m_AudioSourceOnNewBeat;

		private EntityQuery m_EngineQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EngineQuery = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));

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

			m_AudioSourceOnNewBeat = CreateAudioSource("On New Beat (Intro)", 1);
			GetModule(out m_AsyncOpModule);

			m_AudioOnNewBeat = new Dictionary<int, AudioClip>(3);
			for (var i = 0; i <= 3; i++)
				AddAsset($"Effects/Metronome/{i}.wav", new Data {Beat = i});
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i < m_AsyncOpModule.Handles.Count; i++)
			{
				var (handle, data) = DefaultAsyncOperation.InvokeExecute<AudioClip, Data>(m_AsyncOpModule, ref i);
				if (handle.Result == null)
					continue;

				if (handle.Result != null)
					m_AudioOnNewBeat[data.Beat] = handle.Result;
			}

			InitializeValues();

			if (IsNewBeat && m_AudioOnNewBeat != null && m_AudioOnNewBeat.TryGetValue(BeatTarget, out var audioClip))
			{
				m_AudioSourceOnNewBeat.PlayOneShot(audioClip);
			}

			ClearValues();
		}

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

			// don't do intro sounds or if we are ahead of one beat...
			if (engineState.IsPaused || process.GetActivationBeat(settings.BeatInterval) > 0)
				return;

			IsNewBeat  = engineState.IsNewBeat;
			BeatTarget = math.abs(process.GetActivationBeat(settings.BeatInterval));
		}

		private void ClearValues()
		{
			IsNewBeat = false;
		}

		private struct Data
		{
			public int Beat;
		}
	}
}