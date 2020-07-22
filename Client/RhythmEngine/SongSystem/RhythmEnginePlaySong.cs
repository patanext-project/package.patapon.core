using GameBase.Roles.Components;
using GameBase.Roles.Descriptions;
using package.stormiumteam.shared;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PataNext.Client.RhythmEngine.SongSystem
{
	[AlwaysUpdateSystem]
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public partial class RhythmEnginePlaySong : AbsGameBaseSystem
	{
		public SongDescription CurrentSong;
		public bool            HasEngineTarget;

		private AudioSource[] m_BgmSources;
		private AudioSource   m_CommandSource;
		private AudioSource   m_FeverSource;
		private AudioSource   m_CommandVfxSource;
		private EntityQuery   m_EngineQuery;

		private AudioClip[] m_HeroModeChainClips;

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
				audioSource.loop          = true;

				return audioSource;
			}

			m_BgmSources         = new[] {CreateAudioSource("Background Music Primary", 0.75f), CreateAudioSource("Background Music Secondary", 0.75f)};
			m_CommandSource      = CreateAudioSource("Command", 1);
			m_CommandSource.loop = false;

			m_FeverSource      = CreateAudioSource("Fever", 1);
			m_FeverSource.loop = false;

			m_CommandVfxSource      = CreateAudioSource("Vfx Command", 1);
			m_CommandVfxSource.loop = false;

			m_SongSystem = World.GetOrCreateSystem<SongSystem>();

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
			
			var cameraState = this.GetComputedCameraState().StateData;
			if (cameraState.Target != default)
				engine = PlayerComponentFinder.GetComponentFromPlayer<RhythmEngineDescription>(EntityManager, m_EngineQuery, cameraState.Target, player);
			else
				engine = PlayerComponentFinder.FindPlayerComponent(m_EngineQuery, player);

			if (engine == default)
			{
				BgmFeverChain = 0;
				HeroModeSequence = 0;
				IsCommand = false;
				return;
			}

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
			var tmp = serverCommandState.StartTime <= EngineProcess.Milliseconds && serverCommandState.EndTime > EngineProcess.Milliseconds
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
				if (ComboState.IsFever)
					BgmFeverChain++;
				else
					BgmFeverChain = 0;

				// if it's not in hero mode it will be unvalidated later...
				HeroModeSequence++;
			}

			IsCommand = tmp;
			EntityManager.TryGetComponentData(currentCommand.CommandTarget, out TargetCommandDefinition);

			if (EntityManager.HasComponent<RhythmHeroState>(engine))
			{
				var heroModeActive = false;
				var allocResponse  = UnsafeAllocation.From(ref heroModeActive);
				var allocSource    = UnsafeAllocation.From(ref HeroModeSourceUnit);

				new FindActiveHeroMode
				{
					TargetEngine       = engine,
					ReturnAlloc        = allocResponse,
					SourceUnitAlloc    = allocSource,
					SourceAbilityAlloc = UnsafeAllocation.From<Entity>(ref HeroModeSourceAbility),

					ActivationFromEntity = GetComponentDataFromEntity<AbilityActivation>(true),
					StateFromEntity      = GetComponentDataFromEntity<AbilityState>(true)
				}.Run(this);

				if (heroModeActive)
				{
					HeroModeSequence = math.max(0, HeroModeSequence);
				}
				else
					HeroModeSequence = -1;
			}
			else
				HeroModeSequence = -1;
		}

		private void ClearValues()
		{
			HasEngineTarget  = false;
			IsNewBeat        = false;
			IsNewCommand     = false;
		}

		[RequireComponentTag(typeof(UnitDescription))]
		[BurstCompile]
		private struct FindActiveHeroMode : IJobForEachWithEntity_EBC<ActionContainer, Relative<RhythmEngineDescription>>
		{
			public Entity                 TargetEngine;
			public UnsafeAllocation<bool> ReturnAlloc;
			public UnsafeAllocation<Entity> SourceUnitAlloc;
			public UnsafeAllocation<Entity> SourceAbilityAlloc;

			[ReadOnly] public ComponentDataFromEntity<AbilityActivation> ActivationFromEntity;
			[ReadOnly] public ComponentDataFromEntity<AbilityState> StateFromEntity;

			public void Execute(Entity ent, int entIndex, DynamicBuffer<ActionContainer> actionContainer, ref Relative<RhythmEngineDescription> rhythmRelative)
			{
				// this is pretty fast...
				if (rhythmRelative.Target != TargetEngine)
					return;

				var length = actionContainer.Length;
				for (var i = 0; i != length; i++)
				{
					if (!ActivationFromEntity.TryGet(actionContainer[i].Target, out var activation) || activation.Type != EActivationType.HeroMode)
						continue;

					var state = StateFromEntity[actionContainer[i].Target];
					if ((state.Phase & (EAbilityPhase.WillBeActive | EAbilityPhase.HeroActivation | EAbilityPhase.ActiveOrChaining)) != 0)
					{
						ReturnAlloc.Value = true;
						SourceUnitAlloc.Value = ent;
						SourceAbilityAlloc.Value = actionContainer[i].Target;
						return;
					}
				}
			}
		}
	}
}