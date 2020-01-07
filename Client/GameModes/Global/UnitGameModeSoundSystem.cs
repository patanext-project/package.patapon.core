using System.Collections.Generic;
using DefaultNamespace;
using package.patapon.core.Animation.Units;
using package.stormiumteam.shared.ecs;
using Patapon.Client.Graphics.Animation.Units;
using Patapon.Mixed.GameModes;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Systems;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameModes.Global
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class UnitGameModeSoundSystem : GameBaseSystem
	{
		public enum TargetAudio
		{
			Reborn,
			Death
		}

		public struct OperationHandleData
		{
			public TargetAudio type;
		}

		private AudioSource m_AudioSource;

		/// <summary>
		/// Default audio clips, uberhero/darkhero/... archetypes can contains custom audio clips
		/// </summary>
		private Dictionary<TargetAudio, AudioClip> m_AudioClips;

		private AsyncOperationModule m_AsyncOp;

		protected override void OnCreate()
		{
			base.OnCreate();

			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;

				return audioSource;
			}

			m_AudioSource = CreateAudioSource("UnitGameModeSound", 1);
			m_AudioClips  = new Dictionary<TargetAudio, AudioClip>();

			GetModule(out m_AsyncOp);

			var address = AddressBuilder.Client()
			                            .Folder("Sounds")
			                            .Folder("InGame")
			                            .Folder("hero");
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(address.GetFile("uh_def_death.wav")), new OperationHandleData {type  = TargetAudio.Death});
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(address.GetFile("uh_def_reborn.wav")), new OperationHandleData {type = TargetAudio.Reborn});
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOp.Handles.Count; i++)
			{
				var (handle, data) = DefaultAsyncOperation.InvokeExecute<AudioClip, OperationHandleData>(m_AsyncOp, ref i);
				if (handle.Result == null)
					continue;

				m_AudioClips[data.type] = handle.Result;
			}

			GameModeHudSettings hudSettings    = default;
			var                 hasHudSettings = HasSingleton<GameModeHudSettings>();
			if (hasHudSettings)
			{
				hudSettings = GetSingleton<GameModeHudSettings>();
			}

			Entities.ForEach((UnitVisualBackend backend) =>
			{
				// todo: try get audio from backend to replace default audio of this system....

				if (!EntityManager.TryGetComponentData(backend.BackendEntity, out State state))
					EntityManager.AddComponentData(backend.BackendEntity, new State { });

				if (EntityManager.TryGetComponentData(backend.DstEntity, out LivableHealth health))
				{
					var didChange = health.IsDead != state.IsDead;
					if (didChange && hudSettings.EnableUnitSounds)
					{
						AudioClip sound;
						if (health.IsDead && m_AudioClips.TryGetValue(TargetAudio.Death, out sound))
							m_AudioSource.PlayOneShot(sound);
						if (!health.IsDead && m_AudioClips.TryGetValue(TargetAudio.Reborn, out sound))
							m_AudioSource.PlayOneShot(sound);
					}

					state.IsDead = health.IsDead;
				}
				
				EntityManager.SetComponentData(backend.BackendEntity, state);
			});
		}

		public struct State : IComponentData
		{
			public bool IsDead;
		}
	}
}