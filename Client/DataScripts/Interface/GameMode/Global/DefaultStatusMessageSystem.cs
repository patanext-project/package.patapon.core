using System.Collections.Generic;
using DefaultNamespace;
using package.patapon.core.Animation.Units;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GameModes;
using Patapon.Mixed.GameModes.VSHeadOn;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DataScripts.Interface.GameMode.Global
{
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class DefaultStatusMessageSystem : AbsGameBaseSystem
	{
		private AudioSource          m_AudioSource;
		private AsyncOperationModule m_AsyncOp;

		private Dictionary<string, AudioClip> m_Clips;

		private struct Handle
		{
			public string Key;

			public Handle(string k)
			{
				Key = k;
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_AsyncOp);

			m_Clips = new Dictionary<string, AudioClip>();

			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;

				return audioSource;
			}

			m_AudioSource = CreateAudioSource("GmEffect", 1);

			var builder = AddressBuilder.Client()
			                            .Folder("Sounds")
			                            .Folder("GameMode")
			                            .Folder("Global");
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(builder.GetFile("comeback.wav")), new Handle("comeback"));
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(builder.GetFile("upset.wav")), new Handle("upset"));
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(builder.GetFile("win.wav")), new Handle("win"));
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(builder.GetFile("loose.wav")), new Handle("loose"));
			m_AsyncOp.Add(Addressables.LoadAssetAsync<AudioClip>(builder.GetFile("capturetower_captured.wav")), new Handle("tower_captured"));

			World.GetOrCreateSystem<GameModeStatusOnUpdate>().OnModifyStatus += (ref GameModeHudSettings hud, Entity spectated) =>
			{
				string clipTarget = null;
				if (hud.StatusMessage.Equals("comeback_upset") && int.TryParse(hud.StatusMessageArg0.ToString(), out var teamInLead))
				{
					if (!HasSingleton<MpVersusHeadOn>())
						return;
					var gm = GetSingleton<MpVersusHeadOn>();

					var comeback = EntityManager.TryGetComponentData(spectated, out Relative<TeamDescription> teamDesc)
					               && teamDesc.Target == (teamInLead == 0 ? gm.Team0 : gm.Team1);

					if (comeback)
					{
						hud.StatusMessage = "Comeback!";
						clipTarget        = "comeback";
					}
					else
					{
						hud.StatusMessage = "Upset!";
						clipTarget        = "upset";
					}
				}
				else if (hud.StatusSound == EGameModeStatusSound.FlagCaptured)
				{
					if (!HasSingleton<MpVersusHeadOn>())
						return;
					var gm = GetSingleton<MpVersusHeadOn>();

					var winning = EntityManager.TryGetComponentData(spectated, out Relative<TeamDescription> teamDesc)
					              && teamDesc.Target == (gm.WinningTeam == 0 ? gm.Team0 : gm.Team1);

					if (winning)
						clipTarget = "comeback";
					else
						clipTarget = "upset";
				}
				else if (hud.StatusSound == EGameModeStatusSound.WinningSequence)
				{
					if (!HasSingleton<MpVersusHeadOn>())
						return;
					var gm = GetSingleton<MpVersusHeadOn>();
					
					var winning = EntityManager.TryGetComponentData(spectated, out Relative<TeamDescription> teamDesc)
					              && teamDesc.Target == (gm.WinningTeam == 0 ? gm.Team0 : gm.Team1);

					if (winning)
						clipTarget = "win";
					else
						clipTarget = "loose";
				}

				if (clipTarget != null && m_Clips.TryGetValue(clipTarget, out var clip))
				{
					m_AudioSource.clip = clip;
					m_AudioSource.Play();
				}
			};
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOp.Handles.Count; i++)
			{
				var (handle, data) = DefaultAsyncOperation.InvokeExecute<AudioClip, Handle>(m_AsyncOp, ref i);
				if (handle.Result == null)
					continue;
				m_Clips[data.Key] = handle.Result;
			}
		}
	}
}