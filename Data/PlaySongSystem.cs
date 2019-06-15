using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using package.patapon.core;
using Patapon4TLB.Core;
using Patapon4TLB.Core.json;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Patapon4TLB.Default.Test
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class PlaySongClientSystem : GameBaseSystem
	{
		public int Beat;
		public int Interval;
		public int Tick;

		public bool HasActiveRhythmEngine;
		public bool IsCommand;
		public bool IsNewCommand;
		public bool IsNewBeat;

		public int CmdStartAt;

		public GameComboState PreviousComboState;
		public GameComboState ComboState;

		public int CommandStartBeat;
		public int CommandEndBeat;

		private int m_LastBeat;
		private int m_LastCommandStartBeat;

		protected override void OnUpdate()
		{
			HasActiveRhythmEngine = false;
			IsNewCommand          = false;
			IsNewBeat             = false;

			Entities.WithAll<RhythmEngineSimulateTag>().ForEach((ref RhythmEngineSettings settings, ref RhythmEngineState state, ref RhythmEngineProcess process) =>
			{
				if (process.Beat != m_LastBeat)
				{
					m_LastBeat = process.Beat;
					IsNewBeat  = true;
				}

				Beat = process.Beat;

				Tick     = process.TimeTick;
				Interval = settings.BeatInterval;

				HasActiveRhythmEngine = process.StartTime != 0;
			});

			Entities.WithAll<RhythmEngineSimulateTag>().ForEach((ref GameCommandState gameCommandState, ref RhythmCurrentCommand currentCommand, ref GamePredictedCommandState predictedCommand, ref GameComboState comboState, ref GameComboPredictedClient predictedCombo) =>
			{
				var tmp = gameCommandState.StartBeat <= Beat && gameCommandState.EndBeat > Beat     // server
				          || predictedCommand.StartBeat <= Beat && predictedCommand.EndBeat > Beat; // client

				CommandStartBeat = math.max(predictedCommand.StartBeat, gameCommandState.StartBeat);
				CommandEndBeat   = math.max(gameCommandState.EndBeat, predictedCommand.EndBeat);

				if (tmp && !IsCommand || (IsCommand && CommandStartBeat != m_LastCommandStartBeat))
				{
					m_LastCommandStartBeat = CommandStartBeat;
					IsNewCommand           = true;
				}

				var isClientPrediction = false;
				if (gameCommandState.StartBeat >= currentCommand.ActiveAtBeat)
				{
					ComboState = comboState;
				}
				else
				{
					ComboState         = predictedCombo.State;
					isClientPrediction = true;
				}

				IsCommand = tmp;
			});
		}

		public RhythmCommandData GetCommandData()
		{
			var result = default(RhythmCommandData);
			Entities.WithAll<RhythmEngineSimulateTag>().ForEach((ref RhythmCurrentCommand currentCommand) =>
			{
				if (currentCommand.CommandTarget != default)
					result = EntityManager.GetComponentData<RhythmCommandData>(currentCommand.CommandTarget);
			});

			return result;
		}
	}

	[AlwaysUpdateSystem]
	public class PlaySongSystem : GameBaseSystem
	{
		public Dictionary<string, DescriptionFileJsonData> Files;
		public SongDescription CurrentSong;

		private AudioClip m_FeverClip;
		private AudioClip m_FeverLostClip;

		private AudioSource[] m_BgmSources;
		private AudioSource m_CommandSource;

		protected override void OnCreate()
		{
			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;
				audioSource.loop          = true;

				return audioSource;
			}

			base.OnCreate();

			AudioListener.volume = 0.5f;

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

			m_BgmSources         = new[] {CreateAudioSource("Background Music Primary", 0.75f), CreateAudioSource("Background Music Secondary", 0.75f)};
			m_CommandSource      = CreateAudioSource("Command", 1);
			m_CommandSource.loop = false;

			Addressables.LoadAsset<AudioClip>("int:RhythmEngine/Sounds/voice_fever.wav").Completed += (op) => m_FeverClip     = op.Result;
			Addressables.LoadAsset<AudioClip>("int:RhythmEngine/Sounds/fever_lost.wav").Completed  += (op) => m_FeverLostClip = op.Result;
		}

		private int m_CurrentBeat;
		private int m_Flip;
		private const int SongBeatSize = 8;

		// used to not throw the same audio for the command.
		private Dictionary<int, int> m_CommandChain = new Dictionary<int, int>();
		
		private AudioClip m_LastClip;

		private bool m_BgmWasFever;
		private int m_EndFeverEntranceAt;
		private int m_BgmFeverChain;
		private void UpdateBgm(PlaySongClientSystem clientSystem)
		{
			if (!clientSystem.HasActiveRhythmEngine)
			{
				m_BgmSources[0].Stop();
				m_BgmSources[1].Stop();
				return;
			}

			var score       = 0;
			var isFever     = false;
			var targetAudio = m_LastClip;
			var targetTime  = 0.0f;

			var combo = clientSystem.ComboState;
			if (clientSystem.Beat >= CurrentSong.BgmEntranceClips.Count * SongBeatSize || combo.Chain > 0)
			{
				if (!combo.IsFever)
				{
					m_BgmWasFever = false;
					m_BgmFeverChain = 0;
					if (combo.ChainToFever >= 2 && combo.Score >= 50)
					{
						score = 5;
					}
					else
					{
						score = combo.ChainToFever;
						if (combo.ChainToFever >= 25)
						{
							score += 2;
						}
					}

					var part          = CurrentSong.BgmComboParts[score];
					var commandLength = combo.Chain;

					targetAudio = part.Clips[commandLength % part.Clips.Count];
				}
				else
				{
					if (!m_BgmWasFever)
					{
						targetAudio = CurrentSong.BgmFeverEntranceClips[0];
						m_EndFeverEntranceAt = clientSystem.Beat + CurrentSong.BgmFeverEntranceClips.Count * SongBeatSize;

						m_BgmWasFever = true;
					}
					else if (m_EndFeverEntranceAt < clientSystem.Beat)
					{
						var commandLength = math.max(m_BgmFeverChain - 2, 0);
						targetAudio = CurrentSong.BgmFeverLoopClips[commandLength % CurrentSong.BgmFeverLoopClips.Count];
					}
				}
			}
			else
			{
				var commandLength = clientSystem.Beat != 0 ? clientSystem.Beat / SongBeatSize : 0;
				targetAudio = CurrentSong.BgmEntranceClips[commandLength % CurrentSong.BgmEntranceClips.Count];
			}

			var nextBeatDelay = (((clientSystem.Beat + 1) * clientSystem.Interval) - clientSystem.Tick) * 0.001f;
			if (clientSystem.CommandStartBeat >= clientSystem.Beat) // we have a planned command
			{
				nextBeatDelay = (clientSystem.CommandStartBeat * clientSystem.Interval - clientSystem.Tick) * 0.001f;
			}

			// Check if we should change clips or if we are requested to...
			var hasSwitched = false;
			if (m_LastClip != targetAudio)
			{
				Debug.Log($"Switch from {m_LastClip?.name} to {targetAudio?.name}, delay: {nextBeatDelay} (b: {clientSystem.Beat}, s: {clientSystem.CommandStartBeat})");
				
				m_LastClip = targetAudio;
				if (targetAudio == null)
				{
					m_BgmSources[0].Stop();
					m_BgmSources[1].Stop();
				}
				else
				{
					m_BgmSources[1 - m_Flip].SetScheduledEndTime(AudioSettings.dspTime + nextBeatDelay);
					m_BgmSources[m_Flip].clip  = m_LastClip;
					m_BgmSources[m_Flip].pitch = 1;
					m_BgmSources[m_Flip].time  = 0;

					m_BgmSources[m_Flip].PlayScheduled(AudioSettings.dspTime + nextBeatDelay);

					hasSwitched = true;
				}

				m_Flip = 1 - m_Flip;
			}

			var currBgmSource = m_BgmSources[1 - m_Flip];
			if (currBgmSource.clip != null)
			{
				if (hasSwitched)
				{
					var cmdData = clientSystem.GetCommandData();
					if (cmdData.BeatLength == 3)
					{
						var endBeatDelay = (clientSystem.CommandEndBeat * clientSystem.Interval - clientSystem.Tick) * 0.001f;

						m_BgmSources[m_Flip].clip = currBgmSource.clip;
						m_BgmSources[m_Flip].PlayScheduled(AudioSettings.dspTime + nextBeatDelay);
						m_BgmSources[m_Flip].SetScheduledEndTime(AudioSettings.dspTime + endBeatDelay);
						m_BgmSources[m_Flip].pitch  = 2;
						m_BgmSources[m_Flip].volume = currBgmSource.volume;
						currBgmSource.volume        = 0;
						currBgmSource.time          = 0.5f;
					}
				}
				else if (!clientSystem.IsCommand)
				{
					currBgmSource.volume = m_BgmSources[m_Flip].volume;

					currBgmSource.pitch = 1;
				}
			}
		}

		private bool m_WasFever;
		private void UpdateCommand(PlaySongClientSystem clientSystem)
		{
			if (m_WasFever && !clientSystem.ComboState.IsFever)
			{
				m_WasFever = false;
				
				m_CommandSource.Stop();
				m_CommandSource.clip = m_FeverLostClip;
				m_CommandSource.time = 0;
				m_CommandSource.Play();
			}

			if (!clientSystem.IsNewCommand)
			{
				return;
			}

			var commandTarget = default(AudioClip);
			var data          = clientSystem.GetCommandData();

			var id = data.Identifier.ToString();
			var hash = data.Identifier.GetHashCode();

			if (!m_CommandChain.ContainsKey(hash))
			{
				m_CommandChain[hash] = 0;
			}

			if (CurrentSong.CommandsAudio.ContainsKey(id))
			{
				var key = SongDescription.CmdKeyNormal;
				if (clientSystem.ComboState.IsFever)
				{
					key = SongDescription.CmdKeyFever;
				}
				else if (clientSystem.ComboState.ChainToFever > 1 && clientSystem.ComboState.Score >= 33)
				{
					key = SongDescription.CmdKeyPreFever;
				}

				if (!clientSystem.ComboState.IsFever)
				{
					m_WasFever = false;
				}

				if (clientSystem.ComboState.IsFever && !m_WasFever)
				{
					m_WasFever    = true;
					commandTarget = m_FeverClip;
				}
				else
				{
					var clips = CurrentSong.CommandsAudio[id][key];
					commandTarget = clips[m_CommandChain[data.Identifier.GetHashCode()] % (clips.Count)];
				}
			}

			if (clientSystem.ComboState.IsFever)
			{
				m_BgmFeverChain++;
			}

			m_CommandChain[hash] = m_CommandChain[hash] + 1;

			if (commandTarget == null)
				return;

			var nextBeatDelay = ((clientSystem.CommandStartBeat) * clientSystem.Interval - clientSystem.Tick) * 0.001f;
			m_CommandSource.clip = commandTarget;
			m_CommandSource.PlayScheduled(AudioSettings.dspTime + nextBeatDelay);
		}

		protected override void OnUpdate()
		{
			if (CurrentSong.AreAddressableCompleted && !CurrentSong.IsFinalized)
			{
				CurrentSong.FinalizeOperation();
				Debug.Log("Finalize");
			}

			if (!CurrentSong.IsFinalized)
				return;

			var activeClientWorld = GetActiveClientWorld();
			if (activeClientWorld == null)
				return;

			var clientSystem = activeClientWorld.GetOrCreateSystem<PlaySongClientSystem>();

			UpdateBgm(clientSystem);
			UpdateCommand(clientSystem);
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