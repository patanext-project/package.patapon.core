using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
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
			private enum OpType
			{
				BgmFull,
				BgmSlice,
				Command,
			}

			private enum OpBgmSliceType
			{
				NormalEntrance,
				Normal,
				FeverEntrance,
				Fever
			}

			private struct OperationData
			{
				public OpType Type;

				public OpBgmSliceType BgmSliceType;
				public int            BgmSliceNormalCmdRank;
				public int            BgmSliceOrder;
			}

			public struct BgmComboPart
			{
				public int ScoreNeeded;
				public int Start, End;
			}

			private List<IAsyncOperation> m_AddrOperations;
			private List<OperationData>   m_OperationData;
			private bool                  m_IsFinalized;

			public bool AreAddressableCompleted
			{
				get
				{
					var done = 0;
					for (var i = 0; i != m_AddrOperations.Count; i++)
						if (m_AddrOperations[i].IsDone)
							done++;
					return done == m_AddrOperations.Count;
				}
			}

			public bool IsFinalized => m_IsFinalized;

			public readonly DescriptionFileJsonData File;

			// example of the possibilities: normal, pre-fever, fever
			public Dictionary<string, Dictionary<string, List<AudioClip>>> CommandsAudio;

			public AudioClip          Bgm;
			public int                BgmFeverEntrance;
			public int                BgmFeverLoopStart;
			public List<BgmComboPart> BgmComboParts;

			public SongDescription(DescriptionFileJsonData file)
			{
				File = file;

				m_AddrOperations = new List<IAsyncOperation>();
				m_OperationData  = new List<OperationData>();

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

				var  bgmAudioClipCount = 0;
				bool hasEntrancePart   = false, hasNormalPart = false, hasFeverEntrancePart = false, hasFeverPart = false;
				foreach (var bgm in file.bgmAudioSliced)
				{
					bgmAudioClipCount += bgm.Value.Length;
					if (bgm.Key == "entrance")
						hasEntrancePart = true;
					if (bgm.Key.StartsWith("normal"))
						hasNormalPart = true;
					if (bgm.Key == "fever_entrance")
						hasFeverEntrancePart = true;
					if (bgm.Key == "fever")
						hasFeverPart = true;
				}

				if (!hasNormalPart && !hasFeverPart)
					throw new Exception($"e: {hasEntrancePart}, n: {hasNormalPart}, fe: {hasFeverEntrancePart}, f: {hasFeverPart}");

				var order = 0;
				foreach (var bgm in file.bgmAudioSliced)
				{
					foreach (var bgmAudioFile in bgm.Value)
					{
						var data = new OperationData {Type = OpType.BgmSlice, BgmSliceOrder = order};
						if (bgm.Key == "entrance")
						{
							data.BgmSliceType = OpBgmSliceType.NormalEntrance;
						}
						else if (bgm.Key.StartsWith("normal"))
						{
							data.BgmSliceType = OpBgmSliceType.Normal;
							if (bgm.Key == "normal") data.BgmSliceNormalCmdRank = 0;
							else if (bgm.Key.StartsWith("normal_"))
							{
								var strRank = bgm.Key.Replace("normal_", string.Empty);
								if (int.TryParse(strRank, out var rank))
								{
									data.BgmSliceNormalCmdRank = rank;
								}
							}
						}
						else if (bgm.Key == "fever_entrance")
						{
							data.BgmSliceType = OpBgmSliceType.FeverEntrance;
						}
						else if (bgm.Key == "fever")
						{
							data.BgmSliceType = OpBgmSliceType.Fever;
						}

						var op = Addressables.LoadAsset<AudioClip>(bgmAudioFile.Replace("{p}", $"songs:{file.identifier}/bgm/"));

						m_OperationData.Add(data);
						m_AddrOperations.Add(op);

						order++;
					}
				}

				/*foreach (var audio in normalAudio)
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

				BgmFeverLoopStart = order + 1;
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
				}*/
			}

			public void FinalizeOperation()
			{
				if (!AreAddressableCompleted)
					throw new InvalidOperationException("We haven't loaded all addressable asset yet!");

				var bgmPriorities = new NativeList<PriorityBgmSlice>(Allocator.TempJob);
				var bgmAudioClips = new List<AudioClip>();

				var count = m_AddrOperations.Count;
				for (var op = 0; op != count; op++)
				{
					var opAddr = m_AddrOperations[op];
					var opData = m_OperationData[op];

					if (!opAddr.IsValid)
					{
						Debug.Log($"An operation is not valid. (status={opAddr.Status})");
						continue;
					}

					switch (opData.Type)
					{
						case OpType.BgmSlice:
						{
							bgmPriorities.Add(new PriorityBgmSlice
							{
								OpIndex = op,

								Type    = opData.BgmSliceType,
								CmdRank = opData.BgmSliceNormalCmdRank,
								Order   = opData.BgmSliceOrder
							});
							break;
						}
					}
				}

				((NativeArray<PriorityBgmSlice>) bgmPriorities).Sort();
				bgmAudioClips.Capacity = bgmPriorities.Length;

				for (var i = 0; i != bgmPriorities.Length; i++)
				{
					bgmAudioClips.Add((AudioClip) m_AddrOperations[bgmPriorities[i].OpIndex].Result);
				}

				bgmPriorities.Dispose();

				Bgm = AudioClipUtility.Combine("Combined", bgmAudioClips.ToArray());

				m_IsFinalized = true;
			}

			public void Dispose()
			{
				for (var i = 0; i != m_AddrOperations.Count; i++)
				{
					m_AddrOperations[i].Release();
					if (m_AddrOperations[i].Result != null)
						Addressables.ReleaseAsset(m_AddrOperations[i].Result);
				}
			}

			private struct PriorityBgmSlice : IComparable<PriorityBgmSlice>
			{
				public OpBgmSliceType Type;
				public int            CmdRank;
				public int            Order;

				public int OpIndex;

				public int CompareTo(PriorityBgmSlice other)
				{
					if (Type > other.Type)
						return 1;

					if (CmdRank > other.CmdRank)
						return 1;

					return Order > other.Order ? 1 : -1;
				}
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