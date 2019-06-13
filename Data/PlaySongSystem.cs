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

		public int CommandStartBeat;

		private int m_LastBeat;
		private int m_LastCommandStartBeat;

		protected override void OnUpdate()
		{
			HasActiveRhythmEngine = false;
			IsNewCommand = false;
			IsNewBeat = false;
			
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

			Entities.WithAll<RhythmEngineSimulateTag>().ForEach((ref GameCommandState gameCommandState, ref RhythmCurrentCommand currentCommand, ref GamePredictedCommandState predictedCommand) =>
			{
				var tmp = gameCommandState.StartBeat <= Beat && gameCommandState.EndBeat > Beat      // server
				            || currentCommand.ActiveAtBeat <= Beat && predictedCommand.EndBeat > Beat; // client

				CommandStartBeat = math.max(currentCommand.ActiveAtBeat, gameCommandState.StartBeat);

				if (tmp && !IsCommand || (IsCommand && CommandStartBeat != m_LastCommandStartBeat))
				{
					m_LastCommandStartBeat = CommandStartBeat;
					IsNewCommand = true;
				}

				IsCommand = tmp;
			});
		}

		public RhythmCommandData GetCommandData()
		{
			if (!IsCommand)
				throw new InvalidOperationException("???");

			var result = default(RhythmCommandData);
			Entities.WithAll<RhythmEngineSimulateTag>().ForEach((ref RhythmCurrentCommand currentCommand) =>
			{
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

			m_BgmSources         = new[] {CreateAudioSource("Background Music Primary", 1), CreateAudioSource("Background Music Secondary", 1)};
			m_CommandSource      = CreateAudioSource("Command", 1);
			m_CommandSource.loop = false;
		}

		private int m_CurrentBeat;
		private int m_Flip;
		private const int SongBeatSize = 8;

		private AudioClip m_LastClip;

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
			var targetAudio = default(AudioClip);
			var targetTime  = 0.0f;

			if (clientSystem.Beat >= CurrentSong.BgmEntranceClips.Count * SongBeatSize)
			{
				var part          = CurrentSong.BgmComboParts[score];
				var commandLength = clientSystem.Beat != 0 ? clientSystem.Beat / SongBeatSize : 0;
				//targetAudio = part.Clips[commandLength % part.Clips.Count];
				
				targetAudio = CurrentSong.BgmFeverLoopClips[commandLength % CurrentSong.BgmFeverLoopClips.Count];
			}
			else
			{
				var commandLength = clientSystem.Beat != 0 ? clientSystem.Beat / SongBeatSize : 0;
				targetAudio = CurrentSong.BgmEntranceClips[commandLength % CurrentSong.BgmEntranceClips.Count];
			}

			var nextBeatDelay = (((clientSystem.Beat + 1) * clientSystem.Interval) - clientSystem.Tick) * 0.001f;

			// Check if we should change clips...
			if (m_LastClip != targetAudio)
			{
				m_LastClip = targetAudio;
				if (targetAudio == null)
				{
					m_BgmSources[0].Stop();
					m_BgmSources[1].Stop();
				}
				else
				{
					m_BgmSources[1 - m_Flip].SetScheduledEndTime(AudioSettings.dspTime + nextBeatDelay);
					m_BgmSources[m_Flip].clip = m_LastClip;
					
					m_BgmSources[m_Flip].PlayScheduled(AudioSettings.dspTime + nextBeatDelay);
				}

				m_Flip = 1 - m_Flip;
			}

			var currBgmSource = m_BgmSources[1 - m_Flip];
			if (currBgmSource.clip != null)
			{
				
			}
		}

		private void UpdateCommand(PlaySongClientSystem clientSystem)
		{
			if (!clientSystem.IsCommand)
			{
				if (m_CommandSource.isPlaying)
					m_CommandSource.Stop();
			}
			
			if (!clientSystem.IsNewCommand)
			{
				return;
			}

			var commandTarget = default(AudioClip);
			var data          = clientSystem.GetCommandData();

			var id = data.Identifier.ToString();
			if (CurrentSong.CommandsAudio.ContainsKey(id))
			{
				commandTarget = CurrentSong.CommandsAudio[id][SongDescription.CmdKeyFever][0];
			}

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