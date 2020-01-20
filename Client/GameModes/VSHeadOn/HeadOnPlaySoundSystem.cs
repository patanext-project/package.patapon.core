using System.Collections.Generic;
using package.patapon.core.Animation.Units;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DataScripts.Interface.GameMode.VSHeadOn
{
	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class HeadOnPlaySoundSystem : GameBaseSystem
	{
		public struct Data
		{
			public string Key;
		}

		private AsyncOperationModule m_AsyncOp;
		private Dictionary<string, AudioClip> m_Clips; 
		
		private AudioSource m_AudioSource;

		protected override void OnCreate()
		{
			base.OnCreate();
			
			GetModule(out m_AsyncOp);

			void AddAsset(string path, string key)
			{
				m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>($"core://Client/Sounds/GameMode/Global/" + path), new Data {Key = key});
			}

			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;

				return audioSource;
			}

			m_AudioSource = CreateAudioSource("VSHeadOn AudioSource", 1);

			AddAsset("almost_no_time_counter.wav", "less_than_10");
			AddAsset("almost_no_time_left.wav", "10_remaining");
			
			m_Clips = new Dictionary<string, AudioClip>(2);
		}

		private int m_LastSecond;
		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOp.Handles.Count; i++)
			{
				var (handle, data) = DefaultAsyncOperation.InvokeExecute<AudioClip, Data>(m_AsyncOp, ref i);
				if (handle.Result == null)
					continue;
				m_Clips[data.Key] = handle.Result;
			}

			if (!HasSingleton<MpVersusHeadOn>())
				return;
			if (!HasSingleton<GameModeHudSettings>())
				return;

			var gameMode = EntityManager.GetComponentData<MpVersusHeadOn>(GetSingletonEntity<MpVersusHeadOn>());
			if (gameMode.PlayState != MpVersusHeadOn.State.Playing || gameMode.WinningTeam >= 0)
				return;
			
			var hudSettings = EntityManager.GetComponentData<GameModeHudSettings>(GetSingletonEntity<GameModeHudSettings>());
			
			var endTimeSeconds = gameMode.EndTime / 1000;
			var seconds = endTimeSeconds - (int) GetTick(false).Seconds;

			string targetClip = null;
			if (seconds == 10 || seconds == 60)
				targetClip = "10_remaining";
			else if (seconds < 10)
				targetClip = "less_than_10";
			
			if (seconds != m_LastSecond && targetClip != null && m_Clips.TryGetValue(targetClip, out var clip))
			{
				m_LastSecond = seconds;
				m_AudioSource.clip = clip;
				m_AudioSource.Play();

				m_AudioSource.volume = seconds < 0 ? 0.1f : 1f;
			}
		}
	}
}