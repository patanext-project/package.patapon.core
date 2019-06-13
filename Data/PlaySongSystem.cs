using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using StormiumTeam.GameBase;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Patapon4TLB.Default.Test
{
	public class PlaySongSystem : GameBaseSystem
	{
		[Serializable]
		public struct DescriptionFileJsonData
		{
			public struct BgmAudioFull
			{
				public string filePath;
				public int    feverStartBeat;
			}

			public string name;
			public string description;
			public string identifier;

			public Dictionary<string, string[]>                     bgmAudioSliced;
			public BgmAudioFull?                                    bgmAudioFull;
			public Dictionary<string, Dictionary<string, string[]>> commandsAudio;
		}

		public class SongDescription : IDisposable
		{
			public readonly DescriptionFileJsonData File;
			
			// example of the possibilities: normal, pre-fever, fever
			public Dictionary<string, Dictionary<string, List<AudioClip>>> CommandsAudio;

			public AudioClip Bgm;
			public int FeverStartBeat;

			public SongDescription(DescriptionFileJsonData file)
			{
				File = file;

				CommandsAudio = new Dictionary<string, Dictionary<string, List<AudioClip>>>();
				foreach (var fileCmdAudio in file.commandsAudio)
				{
					CommandsAudio[fileCmdAudio.Key] = new Dictionary<string, List<AudioClip>>();
					foreach (var commands in fileCmdAudio.Value)
					{
						var audioList = CommandsAudio[fileCmdAudio.Key][commands.Key] = new List<AudioClip>();
						for (var i = 0; i != commands.Value.Length; i++)
						{
							var addrPath = commands.Value[i];
							addrPath = addrPath.Replace("{p}", $"songs:{file.identifier}/commands/{fileCmdAudio.Key}/");

							var insertIndex = i;
							Addressables.LoadAsset<AudioClip>(addrPath).Completed += (op) =>
							{
								if (op.Status == AsyncOperationStatus.Failed)
								{
									return;
								}

								audioList.Insert(insertIndex, op.Result);
							};
						}
					}
				}

				var useFullAudio = file.bgmAudioFull != null;
				if (useFullAudio)
				{
					throw new NotImplementedException();
				}

				if (file.bgmAudioSliced == null)
					throw new Exception("sliced and full values are not set!");

				// We actually merge all the parts
				var bgmAudioClips = new List<AudioClip>();
				if (!file.bgmAudioSliced.ContainsKey("normal"))
					throw new Exception("normal key was not set for bgmAudioSliced");


				var normalAudio = file.bgmAudioSliced["normal"];
				var feverAudio  = file.bgmAudioSliced["fever"];

				bgmAudioClips.AddRange(new AudioClip[normalAudio.Length + feverAudio.Length]);

				var order = 0;
				foreach (var audio in normalAudio)
				{
					order++;

					var insertIndex = order;
					Addressables.LoadAsset<AudioClip>(audio.Replace("{p}", $"songs:{file.identifier}/bgm/")).Completed += op =>
					{
						bgmAudioClips.Insert(insertIndex, op.Result);
						
						op.Result.LoadAudioData();

						if (Bgm != null)
						{
							Object.Destroy(Bgm);
						}

						AudioSource.PlayClipAtPoint(op.Result, Vector3.zero, 1);
						Bgm = AudioClipUtility.Combine("Combined", bgmAudioClips.Where(t => t != null).ToArray());
					};
				}

				var feverOrder = order + 1;
				foreach (var audio in feverAudio)
				{
					order++;

					var insertIndex = order;
					Addressables.LoadAsset<AudioClip>(audio.Replace("{p}", $"songs:{file.identifier}/bgm/")).Completed += op =>
					{
						bgmAudioClips.Insert(insertIndex, op.Result);
						if (Bgm != null)
						{
							Object.Destroy(Bgm);
						}

						Bgm = AudioClipUtility.Combine("Combined", bgmAudioClips.Where(t => t != null).ToArray());
					};
				}
			}

			public void Dispose()
			{
			}
		}

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
			
			RequireForUpdate(GetEntityQuery(typeof(Translation)));
		}

		private bool m_playTest;
		protected override void OnUpdate()
		{
			Debug.Log(CurrentSong.Bgm);
			if (!m_playTest && CurrentSong.Bgm != null)
			{
				m_playTest = true;

				Debug.Log("State: " + CurrentSong.Bgm.loadState);
				CurrentSong.Bgm.LoadAudioData();
				
				m_BgmSource.PlayOneShot(CurrentSong.Bgm);
				SaveAudioToWav.Save(Application.streamingAssetsPath + "/combined.bgm", CurrentSong.Bgm);
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