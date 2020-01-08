using Misc;
using Misc.Extensions;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.GamePlay.Units;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Patapon.Client.RhythmEngine
{
	[AlwaysUpdateSystem]
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public partial class RhythmEnginePlaySong : GameBaseSystem
	{
		public SongDescription CurrentSong;
		public bool            HasEngineTarget;

		private AudioSource[] m_BgmSources;
		private AudioSource   m_CommandSource;
		private AudioSource   m_CommandVfxSource;
		private EntityQuery   m_EngineQuery;

		private AudioClip m_FeverClip;
		private AudioClip m_FeverLostClip;

		private int m_LastCommandStartTime;

		private string m_PreviousSongId;

		private SongSystem m_SongSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EngineQuery = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));

			AudioSource CreateAudioSource(string name, float volume)
			{
				var audioSource = new GameObject("(Clip) " + name, typeof(AudioSource)).GetComponent<AudioSource>();
				audioSource.reverbZoneMix = 0f;
				audioSource.spatialBlend  = 0f;
				audioSource.volume        = volume;

				return audioSource;
			}

			m_BgmSources         = new[] {CreateAudioSource("Background Music Primary", 0.75f), CreateAudioSource("Background Music Secondary", 0.75f)};
			m_CommandSource      = CreateAudioSource("Command", 1);
			m_CommandSource.loop = false;

			m_CommandVfxSource      = CreateAudioSource("Vfx Command", 1);
			m_CommandVfxSource.loop = false;

			m_SongSystem                 = World.GetOrCreateSystem<SongSystem>();
			m_SongSystem.MapTargetSongId = "test_song";

			RegisterAsyncOperations();
		}

		protected override void OnUpdate()
		{
			UpdateAsyncOperations();

			if (m_PreviousSongId != m_SongSystem.MapTargetSongId)
			{
				m_PreviousSongId = m_SongSystem.MapTargetSongId;
				CurrentSong?.Dispose();

				if (m_PreviousSongId != null && m_SongSystem.Files.ContainsKey(m_PreviousSongId)) CurrentSong = new SongDescription(m_SongSystem.Files[m_PreviousSongId]);
			}

			if (CurrentSong?.IsFinalized == false)
				return;

			InitializeValues();
			Render();
			ClearValues();
		}

		private void InitializeValues()
		{
			var player = this.GetFirstSelfGamePlayer();
			if (player == default)
				return;

			Entity engine;
			if (this.TryGetCurrentCameraState(player, out var camState))
				engine = PlayerComponentFinder.GetComponentFromPlayer<RhythmEngineDescription>(EntityManager, m_EngineQuery, camState.Target, player);
			else
				engine = PlayerComponentFinder.FindPlayerComponent(m_EngineQuery, player);

			if (engine == default)
				return;

			var currentCommand     = EntityManager.GetComponentData<RhythmCurrentCommand>(engine);
			var serverCommandState = EntityManager.GetComponentData<GameCommandState>(engine);
			var clientCommandState = EntityManager.GetComponentData<GamePredictedCommandState>(engine);
			var engineState        = EntityManager.GetComponentData<RhythmEngineState>(engine);
			EngineProcess  = EntityManager.GetComponentData<FlowEngineProcess>(engine);
			EngineSettings = EntityManager.GetComponentData<RhythmEngineSettings>(engine);

			// don't do player sounds if it's paused or it didn't started yet
			if (engineState.IsPaused || EngineProcess.Milliseconds <= 0)
				return;

			HasEngineTarget = true;
			IsNewBeat       = engineState.IsNewBeat;

			if (serverCommandState.StartTime >= currentCommand.ActiveAtTime)
				ComboState = EntityManager.GetComponentData<GameComboState>(engine);
			else
				ComboState = EntityManager.GetComponentData<GameComboPredictedClient>(engine).State;

			var isCommandClient                                                          = false;
			var isCommandServer                                                          = serverCommandState.StartTime <= EngineProcess.Milliseconds && serverCommandState.EndTime > EngineProcess.Milliseconds;
			if (EntityManager.HasComponent<FlowSimulateProcess>(engine)) isCommandClient = currentCommand.ActiveAtTime <= EngineProcess.Milliseconds && clientCommandState.State.EndTime > EngineProcess.Milliseconds;

			var tmp = serverCommandState.StartTime <= EngineProcess.Milliseconds && serverCommandState.StartTime > EngineProcess.Milliseconds
			          || clientCommandState.State.StartTime <= EngineProcess.Milliseconds && clientCommandState.State.EndTime > EngineProcess.Milliseconds;

			CommandStartTime = math.max(clientCommandState.State.StartTime, serverCommandState.StartTime);
			CommandEndTime   = math.max(clientCommandState.State.EndTime, serverCommandState.EndTime);
			if (tmp && !IsCommand || IsCommand && CommandStartTime != m_LastCommandStartTime)
			{
				m_LastCommandStartTime = CommandStartTime;
				IsNewCommand           = true;
			}

			if (IsNewCommand)
			{
				Debug.Log("New command!");
				
				if (ComboState.IsFever)
					BgmFeverChain++;
				else
					BgmFeverChain = 0;
			}

			IsCommand = tmp;
			EntityManager.TryGetComponentData(currentCommand.CommandTarget, out TargetCommandDefinition);
		}

		private void ClearValues()
		{
			HasEngineTarget = false;
			IsNewBeat       = false;
			IsNewCommand    = false;
		}
	}
}