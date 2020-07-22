using System.Text;
using GameBase.Roles.Components;
using GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.Shared;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Entity = Unity.Entities.Entity;

namespace DataScripts.Interface.GameMode.Global
{
	public class UIGameModeIntroPresentation : RuntimeAssetPresentation<UIGameModeIntroPresentation>
	{
		public Animator backgroundAnimator;
		public Animator countdownAnimator;

		public TextMeshProUGUI countdownLabel;
		
		public Color countdownColor;
		public Color goColor;
	}

	public class UIGameModeIntroBackend : RuntimeAssetBackend<UIGameModeIntroPresentation>
	{
		public override bool PresentationWorldTransformStayOnSpawn => false;
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class UIGameModeIntroRenderSystem : BaseRenderSystem<UIGameModeIntroPresentation>
	{
		private static readonly int         IsShownHashId = Animator.StringToHash("IsShown");
		private                 EntityQuery m_EngineQuery;

		public int  CounterTarget;
		public bool IsShown, IsNewBeat;

		private StringBuilder m_Sb;
		private Localization m_LocalDb;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_EngineQuery = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));
			m_Sb = new StringBuilder();

			m_LocalDb = World.GetOrCreateSystem<LocalizationSystem>()
			                 .LoadLocal("ingame_gamemode");
		}

		protected override void PrepareValues()
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
				return;

			var engineState = EntityManager.GetComponentData<RhythmEngineState>(engine);
			var process     = EntityManager.GetComponentData<FlowEngineProcess>(engine);
			var settings    = EntityManager.GetComponentData<RhythmEngineSettings>(engine);

			// don't do intro sounds or if we are ahead of one beat...
			if (engineState.IsPaused || process.GetActivationBeat(settings.BeatInterval) > 0)
				return;

			IsShown = true;

			var targetBeat = math.abs(process.GetActivationBeat(settings.BeatInterval));
			if (targetBeat != CounterTarget)
			{
				CounterTarget = targetBeat;
				IsNewBeat = true;
			}
			
			m_Sb.Clear();
			if (CounterTarget <= 3)
			{
				if (CounterTarget != 0)
					m_Sb.Append(CounterTarget);
				else
					m_Sb.Append(m_LocalDb["Go!", "Global|GM"]);
			}
		}

		protected override void Render(UIGameModeIntroPresentation definition)
		{
			definition.backgroundAnimator.SetBool(IsShownHashId, IsShown);
			definition.countdownLabel.SetText(m_Sb);
			if (IsNewBeat)
			{
				definition.countdownAnimator.SetTrigger("Trigger");
				definition.countdownLabel.color = CounterTarget == 0 ? definition.goColor : definition.countdownColor;
			}
		}

		protected override void ClearValues()
		{
			IsShown   = false;
			IsNewBeat = false;
		}
	}
}