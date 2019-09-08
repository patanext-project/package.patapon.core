using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using package.patapon.core;
using Patapon4TLB.Core;
using Patapon4TLB.Core.json;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Revolution.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Patapon4TLB.Default.Test
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class PlaySongClientSystem : GameBaseSystem
	{
		public int FlowBeat;
		public int ActivationBeat;
		public int Interval;
		public int ProcessMs;

		public bool HasActiveRhythmEngine;
		public bool IsCommand;
		public bool IsNewCommand;
		public bool IsNewBeat;

		public int CmdStartAt;

		public GameComboState PreviousComboState;
		public GameComboState ComboState;

		public int CommandStartTime;
		public int CommandEndTime;

		private int m_PreviousActivationBeat;
		private int m_LastCommandStartTime;

		protected override void OnUpdate()
		{
			HasActiveRhythmEngine = false;
			IsNewCommand          = false;
			IsNewBeat             = false;

			Entities.WithAll<RhythmEngineSimulateTag>().ForEach((ref RhythmEngineSettings settings, ref RhythmEngineState state, ref RhythmEngineProcess process) =>
			{
				var activeBeat = process.GetActivationBeat(settings.BeatInterval);
				if (activeBeat != m_PreviousActivationBeat)
				{
					m_PreviousActivationBeat = activeBeat;
					IsNewBeat                = true;
				}

				ActivationBeat = activeBeat;
				FlowBeat       = process.GetFlowBeat(settings.BeatInterval);

				ProcessMs = process.Milliseconds;
				Interval  = settings.BeatInterval;

				// should we also set it to false for the condition 'process.Milliseconds < 0' ?
				if (state.IsPaused || process.Milliseconds < 0)
					return;
				HasActiveRhythmEngine = true;
			});

			Entities.WithAll<RhythmEngineSimulateTag>().ForEach((ref GameCommandState gameCommandState, ref RhythmCurrentCommand currentCommand, ref GamePredictedCommandState predictedCommand, ref GameComboState comboState, ref GameComboPredictedClient predictedCombo) =>
			{
				var tmp = gameCommandState.StartTime <= ProcessMs && gameCommandState.StartTime > ProcessMs               // server
				          || predictedCommand.State.StartTime <= ProcessMs && predictedCommand.State.EndTime > ProcessMs; // client

				CommandStartTime = math.max(predictedCommand.State.StartTime, gameCommandState.StartTime);
				CommandEndTime   = math.max(gameCommandState.EndTime, predictedCommand.State.EndTime);

				if (tmp && !IsCommand || (IsCommand && CommandStartTime != m_LastCommandStartTime))
				{
					m_LastCommandStartTime = CommandStartTime;
					IsNewCommand           = true;
				}

				var isClientPrediction = false;
				if (gameCommandState.StartTime >= currentCommand.ActiveAtTime)
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
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[NotClientServerSystem]
	public class PlaySongSystem : GameBaseSystem
	{
		public Dictionary<string, DescriptionFileJsonData> Files;
		public SongDescription                             CurrentSong;

		private AudioClip m_FeverClip;
		private AudioClip m_FeverLostClip;

		private AudioSource[] m_BgmSources;
		private AudioSource   m_CommandSource;
		private AudioSource   m_CommandVfxSource;

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

			m_CommandVfxSource      = CreateAudioSource("Vfx Command", 1);
			m_CommandVfxSource.loop = false;

			Addressables.LoadAsset<AudioClip>("int:RhythmEngine/Sounds/voice_fever.wav").Completed += (op) => m_FeverClip     = op.Result;
			Addressables.LoadAsset<AudioClip>("int:RhythmEngine/Sounds/fever_lost.wav").Completed  += (op) => m_FeverLostClip = op.Result;
		}

		private       int m_CurrentBeat;
		private       int m_Flip;
		private const int SongBeatSize = 8;

		// used to not throw the same audio for the command.
		private Dictionary<int, int> m_CommandChain = new Dictionary<int, int>();

		private AudioClip m_LastClip;

		private bool m_BgmWasFever;
		private int  m_EndFeverEntranceAt;
		private int  m_BgmFeverChain;

		private bool m_HadRhythmEngine;
		
		private void UpdateBgm(PlaySongClientSystem clientSystem)
		{
			if (!clientSystem.HasActiveRhythmEngine)
			{
				m_HadRhythmEngine = false;
				
				m_BgmSources[0].Stop();
				m_BgmSources[1].Stop();
				return;
			}

			var forceSongChange = false;
			if (!m_HadRhythmEngine)
			{
				m_HadRhythmEngine = true;
				forceSongChange = true;
			}

			var score       = 0;
			var isFever     = false;
			var targetAudio = m_LastClip;
			var targetTime  = 0.0f;

			var combo = clientSystem.ComboState;
			
			if (clientSystem.ActivationBeat >= CurrentSong.BgmEntranceClips.Count * SongBeatSize || combo.Chain > 0)
			{
				if (!combo.IsFever)
				{
					m_BgmWasFever   = false;
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
						targetAudio          = CurrentSong.BgmFeverEntranceClips[0];
						m_EndFeverEntranceAt = clientSystem.ActivationBeat + CurrentSong.BgmFeverEntranceClips.Count * SongBeatSize;

						m_BgmWasFever = true;
					}
					else if (m_EndFeverEntranceAt < clientSystem.ActivationBeat)
					{
						var commandLength = math.max(m_BgmFeverChain - 2, 0);
						targetAudio = CurrentSong.BgmFeverLoopClips[commandLength % CurrentSong.BgmFeverLoopClips.Count];
					}
				}
			}
			else
			{
				var commandLength = clientSystem.ActivationBeat != 0 ? clientSystem.ActivationBeat / SongBeatSize : 0;
				targetAudio = CurrentSong.BgmEntranceClips[commandLength % CurrentSong.BgmEntranceClips.Count];
			}

			var nextBeatDelay          = (((clientSystem.ActivationBeat + 1) * clientSystem.Interval) - clientSystem.ProcessMs) * 0.001f;
			var cmdStartActivationBeat = RhythmEngineProcess.CalculateActivationBeat(clientSystem.CommandStartTime, clientSystem.Interval);
			if (cmdStartActivationBeat >= clientSystem.ActivationBeat) // we have a planned command
			{
				nextBeatDelay = (cmdStartActivationBeat * clientSystem.Interval - clientSystem.ProcessMs) * 0.001f;
			}

			// Check if we should change clips or if we are requested to...
			var hasSwitched = false;
			if (m_LastClip != targetAudio || forceSongChange)
			{
				Debug.Log($"Switch from {m_LastClip?.name} to {targetAudio?.name}, delay: {nextBeatDelay} (b: {clientSystem.ActivationBeat}, f: {clientSystem.FlowBeat}, s: {cmdStartActivationBeat})");

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
						var cmdEndActivationBeat = RhythmEngineProcess.CalculateActivationBeat(clientSystem.CommandEndTime, clientSystem.Interval);
						var endBeatDelay         = (cmdEndActivationBeat * clientSystem.Interval - clientSystem.ProcessMs) * 0.001f;

						m_BgmSources[m_Flip].clip = currBgmSource.clip;
						m_BgmSources[m_Flip].PlayScheduled(AudioSettings.dspTime + nextBeatDelay);
						m_BgmSources[m_Flip].SetScheduledEndTime(AudioSettings.dspTime + endBeatDelay);
						m_BgmSources[m_Flip].pitch  = 1f;
						m_BgmSources[m_Flip].volume = currBgmSource.volume;
						m_EndTime                   = Time.time + 1.5f;
						currBgmSource.volume        = 0;
						currBgmSource.time          = 0.5f;

						m_IsSkippingSong = true;
					}
				}
				else if (!clientSystem.IsCommand)
				{
					currBgmSource.volume = m_BgmSources[m_Flip].volume;

					currBgmSource.pitch = 1;
				}
			}

			if (m_IsSkippingSong)
			{
				m_BgmSources[m_Flip].pitch += Time.deltaTime;
			}

			if (m_EndTime < Time.time)
			{
				m_IsSkippingSong = false;
			}
		}

		private bool  m_IsSkippingSong;
		private float m_EndTime;

		private bool m_WasFever;

		private void UpdateCommand(PlaySongClientSystem clientSystem)
		{
			if (m_WasFever && !clientSystem.ComboState.IsFever)
			{
				m_WasFever = false;

				m_CommandVfxSource.Stop();
				m_CommandVfxSource.clip = m_FeverLostClip;
				m_CommandVfxSource.time = 0;
				m_CommandVfxSource.Play();
			}

			if (!clientSystem.IsNewCommand)
			{
				if (clientSystem.ComboState.Chain <= 0)
					m_CommandSource.Stop(); // interrupted

				return;
			}

			var commandTarget = default(AudioClip);
			var data          = clientSystem.GetCommandData();

			var id   = data.Identifier.ToString();
			var hash = data.Identifier.GetHashCode();

			if (!m_CommandChain.ContainsKey(hash))
			{
				m_CommandChain[hash] = 0;
			}

			if (CurrentSong.CommandsAudio.ContainsKey(id) && !(clientSystem.ComboState.IsFever && !m_WasFever))
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

				var clips = CurrentSong.CommandsAudio[id][key];
				commandTarget = clips[m_CommandChain[data.Identifier.GetHashCode()] % (clips.Count)];
			}
			else if (clientSystem.ComboState.IsFever && !m_WasFever)
			{
				m_WasFever    = true;
				commandTarget = m_FeverClip;
			}

			if (clientSystem.ComboState.IsFever)
			{
				m_BgmFeverChain++;
			}

			m_CommandChain[hash] = m_CommandChain[hash] + 1;

			if (commandTarget == null)
				return;

			var cmdStartActivationBeat = RhythmEngineProcess.CalculateActivationBeat(clientSystem.CommandStartTime, clientSystem.Interval);
			var nextBeatDelay          = (cmdStartActivationBeat * clientSystem.Interval - clientSystem.ProcessMs) * 0.001f;
			m_CommandSource.clip  = commandTarget;
			m_CommandSource.pitch = data.BeatLength == 3 ? 1.25f : 1f;
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