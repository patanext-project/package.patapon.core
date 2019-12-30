using System.Collections.Generic;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Definitions;
using Patapon.Mixed.RhythmEngine.Flow;
using Unity.Mathematics;
using UnityEngine;

namespace Patapon.Client.RhythmEngine
{
	public partial class RhythmEnginePlaySong
	{
		private const int SongBeatSize = 8;
		
		public int CommandStartTime;
		public int CommandEndTime;
		
		public bool IsNewBeat;
		public bool IsNewCommand;
		public bool IsCommand;

		public bool BgmWasFever;
		public int BgmFeverChain;

		public FlowEngineProcess       EngineProcess;
		public RhythmEngineSettings    EngineSettings;
		public GameComboState          ComboState;
		public RhythmCommandDefinition TargetCommandDefinition;

		private AudioClip m_LastClip;
		
		private bool m_WasFever;
		private bool m_HadRhythmEngine;
		private int m_EndFeverEntranceAt;
		private int m_Flip;
		
		// removed for now
		// check this github link:
		// https://github.com/guerro323/package.patapon.core/blob/a05689eb7f2500964e820daebe07d58cb20c8233/Data/PlaySongSystem.cs
		// for reimplementing it from a base
		//private bool m_IsSkippingSong;
		private double m_EndTime;

		// used to not throw the same audio for the command.
		private Dictionary<int, int> m_CommandChain = new Dictionary<int, int>();

		public void Render()
		{
			RenderBgm();
			RenderCommand();
		}

		private void RenderBgm()
		{
			if (!HasEngineTarget)
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
				forceSongChange   = true;
			}

			int score;
			var targetAudio = m_LastClip;

			var activationBeat = FlowEngineProcess.CalculateActivationBeat(EngineProcess.Milliseconds, EngineSettings.BeatInterval);
			var flowBeat       = FlowEngineProcess.CalculateFlowBeat(EngineProcess.Milliseconds, EngineSettings.BeatInterval);

			if (activationBeat >= CurrentSong.BgmEntranceClips.Count * SongBeatSize || ComboState.Chain > 0)
			{
				if (!ComboState.IsFever)
				{
					BgmWasFever   = false;
					BgmFeverChain = 0;
					if (ComboState.ChainToFever >= 2 && ComboState.Score >= 50)
					{
						score = 5;
					}
					else
					{
						score = ComboState.ChainToFever;
						if (ComboState.ChainToFever >= 25)
						{
							score += 2;
						}
					}

					var part          = CurrentSong.BgmComboParts[score];
					var commandLength = ComboState.Chain;

					targetAudio = part.Clips[commandLength % part.Clips.Count];
				}
				else
				{
					if (!BgmWasFever)
					{
						targetAudio          = CurrentSong.BgmFeverEntranceClips[0];
						m_EndFeverEntranceAt = activationBeat + CurrentSong.BgmFeverEntranceClips.Count * SongBeatSize;

						BgmWasFever = true;
					}
					else if (m_EndFeverEntranceAt < activationBeat)
					{
						var commandLength = math.max(BgmFeverChain - 2, 0);
						targetAudio = CurrentSong.BgmFeverLoopClips[commandLength % CurrentSong.BgmFeverLoopClips.Count];
					}
				}
			}
			else
			{
				var commandLength = activationBeat != 0 ? activationBeat / SongBeatSize : 0;
				targetAudio = CurrentSong.BgmEntranceClips[commandLength % CurrentSong.BgmEntranceClips.Count];
			}

			var nextBeatDelay          = (((activationBeat + 1) * EngineSettings.BeatInterval) - EngineProcess.Milliseconds) * 0.001f;
			var cmdStartActivationBeat = FlowEngineProcess.CalculateActivationBeat(CommandStartTime, EngineSettings.BeatInterval);
			if (cmdStartActivationBeat >= activationBeat) // we have a planned command
			{
				nextBeatDelay = (cmdStartActivationBeat * EngineSettings.BeatInterval - EngineProcess.Milliseconds) * 0.001f;
			}
			
			// Check if we should change clips or if we are requested to...
			var hasSwitched = false;
			if (m_LastClip != targetAudio // switch audio if we are requested to
			    || forceSongChange)       // play an audio if we got forced
			{
				Debug.Log($"Switch from {m_LastClip?.name} to {targetAudio?.name}, delay: {nextBeatDelay} (b: {activationBeat}, f: {flowBeat}, s: {cmdStartActivationBeat})");

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

			// It's loop time
			if (!hasSwitched && targetAudio != null && m_BgmSources[1 - m_Flip].time >= targetAudio.length)
			{
				Debug.Log($"Looping {m_LastClip?.name}");
				m_BgmSources[1 - m_Flip].time = m_BgmSources[1 - m_Flip].time - targetAudio.length;
				m_BgmSources[1 - m_Flip].Play();
			}

			var currBgmSource = m_BgmSources[1 - m_Flip];
			if (currBgmSource.clip == null)
				return;
			if (hasSwitched || IsCommand)
				return;

			currBgmSource.volume = m_BgmSources[m_Flip].volume;

			currBgmSource.pitch = 1;
		}

		private void RenderCommand()
		{
			if (m_WasFever && !ComboState.IsFever)
			{
				m_WasFever = false;

				m_CommandVfxSource.Stop();
				m_CommandVfxSource.clip = m_FeverLostClip;
				m_CommandVfxSource.time = 0;
				m_CommandVfxSource.Play();
			}

			if (!IsNewCommand)
			{
				if (ComboState.Chain <= 0)
					m_CommandSource.Stop(); // interrupted

				return;
			}

			var commandTarget = default(AudioClip);

			var id   = TargetCommandDefinition.Identifier.ToString();
			var hash = TargetCommandDefinition.Identifier.GetHashCode();

			if (!m_CommandChain.ContainsKey(hash))
			{
				m_CommandChain[hash] = 0;
			}

			if (CurrentSong.CommandsAudio.ContainsKey(id) && !(ComboState.IsFever && !m_WasFever))
			{
				var key = SongDescription.CmdKeyNormal;
				if (ComboState.IsFever)
				{
					key = SongDescription.CmdKeyFever;
				}
				else if (ComboState.ChainToFever > 1 && ComboState.Score >= 33)
				{
					key = SongDescription.CmdKeyPreFever;
				}

				if (!ComboState.IsFever)
				{
					m_WasFever = false;
				}

				var clips = CurrentSong.CommandsAudio[id][key];
				commandTarget = clips[m_CommandChain[TargetCommandDefinition.Identifier.GetHashCode()] % (clips.Count)];
			}
			else if (ComboState.IsFever && !m_WasFever)
			{
				m_WasFever    = true;
				commandTarget = m_FeverClip;
			}

			m_CommandChain[hash] = m_CommandChain[hash] + 1;

			Debug.Log(commandTarget);
			if (commandTarget == null)
				return;

			var cmdStartActivationBeat = FlowEngineProcess.CalculateActivationBeat(EngineProcess.StartTime, EngineSettings.BeatInterval);
			var nextBeatDelay          = (cmdStartActivationBeat * EngineSettings.BeatInterval - EngineProcess.Milliseconds) * 0.001f;
			m_CommandSource.clip  = commandTarget;
			m_CommandSource.pitch = TargetCommandDefinition.BeatLength == 3 ? 1.25f : 1f;
			m_CommandSource.PlayScheduled(AudioSettings.dspTime + nextBeatDelay);
		}
	}
}