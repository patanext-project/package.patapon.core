using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Patapon4TLB.Core;
using Patapon4TLB.Core.json;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Patapon4TLB.Default.Test
{
	[AlwaysUpdateSystem]
	public class PlaySongSystem : GameBaseSystem
	{
		public Dictionary<string, DescriptionFileJsonData> Files;
		public SongDescription CurrentSong;

		private AudioSource m_BgmSource;

		protected override void OnCreate()
		{
			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;

				return audioSource;
			}
			
			base.OnCreate();
			
			Files = new Dictionary<string, DescriptionFileJsonData>();

			var songFiles = Directory.GetFiles(Application.streamingAssetsPath + "/songs", "*.json", SearchOption.TopDirectoryOnly);
			foreach (var file in songFiles)
			{
				try
				{
					var obj = JsonConvert.DeserializeObject<DescriptionFileJsonData>(File.ReadAllText(file));
						Debug.Log($"Found song: (id={obj.identifier}, name={obj.name})");

					Files[obj.identifier] = obj;
					
					LoadSong(obj.identifier);
				}
				catch (Exception ex)
				{
					Debug.LogError("Couldn't parse song file: " + file);
					Debug.LogException(ex);
				}
			}

			m_BgmSource = CreateAudioSource("Bgm", 1);
		}

		private bool m_playTest;
		protected override void OnUpdate()
		{
			if (CurrentSong.AreAddressableCompleted && !CurrentSong.IsFinalized)
			{
				CurrentSong.FinalizeOperation();
			}
			
			if (!m_playTest && CurrentSong.AreAddressableCompleted)
			{
				m_playTest = true;

				CurrentSong.Bgm.LoadAudioData();
				m_BgmSource.clip = CurrentSong.Bgm;
				m_BgmSource.Play();
			}
		}

		public void LoadSong(string fileId)
		{
			LoadSong(Files[fileId]);
		}

		public void LoadSong(DescriptionFileJsonData file)
		{
			if (CurrentSong != null)
			{
				CurrentSong.Dispose();
			}
			
			CurrentSong = new SongDescription(file);
		}
	}
}