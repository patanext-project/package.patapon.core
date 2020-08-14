using System;
using package.stormiumteam.shared.ecs;
using PataNext.Client.OrderSystems;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Simulation.mixed.Components.GamePlay.RhythmEngine;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems.Ext;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.DOTS;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using StormiumTeam.GameBase.Utility.Rendering;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using StormiumTeam.GameBase.Utility.uGUI.Systems;
using StormiumTeam.Shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Models.RhythmEngine.FeverWorm
{
	public class FeverWormBackend : RuntimeAssetBackend<FeverWormPresentation>
	{
		private bool? m_IsEnabled;

		public void SetEnabled(bool value)
		{
			if (m_IsEnabled == null || m_IsEnabled.Value != value)
			{
				m_IsEnabled = value;
				Presentation.gameObject.SetActive(value);
			}
		}

		public override void OnReset()
		{
			base.OnReset();
			m_IsEnabled = null;
		}
	}

	// we do process on the backend instead of presentation since we do disable/enabled the presentation (aka removing it from the entity list)
	[UpdateInGroup(typeof(OrderGroup.Presentation.InterfaceRendering))]
	public class FeverWormRenderSystem : BaseRenderSystem<FeverWormBackend>
	{
		private static readonly int _NtOneBeat    = Animator.StringToHash("_NT1.0");
		private static readonly int _NtDoubleBeat = Animator.StringToHash("_NT0.5");
		private static readonly int _Score        = Animator.StringToHash("_Score");
		private static readonly int _IsFever      = Animator.StringToHash("_IsFever");

		public int   ComboCount;
		public float ComboScoreReal;

		public string ComboString;
		public string FeverString;
		
		public float  InterpolatedEnergyReal;

		public bool IsFever;

		private EntityQuery          m_EngineQuery;
		private Localization         m_LocalTextDb;

		private int   m_PreviousScore;
		private float m_PreviousScoreInterpol;

		public float Pulsation;

		public float SummonEnergyReal;

		private float real(int v, int m)
		{
			if (m == 0 || v == m)
				return 1f;
			if (v == 0)
				return 0f;
			return (float) v / m;
		}

		protected override void PrepareValues()
		{
			if (m_LocalTextDb == null)
				m_LocalTextDb = World.GetOrCreateSystem<LocalizationSystem>()
				                     .LoadLocal("ingame_interface");

			ComboString = m_LocalTextDb["ComboLabel", "FWorm"];
			FeverString = m_LocalTextDb["FeverLabel", "FWorm"];

			var player = this.GetFirstSelfGamePlayer();
			if (player == default)
				return;

			var cameraState = this.GetComputedCameraState().StateData;

			Entity engine;
			if (cameraState.Target != default)
				engine = PlayerComponentFinder.GetRelativeChild<RhythmEngineDescription>(EntityManager, m_EngineQuery, cameraState.Target, player);
			else
				engine = PlayerComponentFinder.FromQueryFindPlayerChild(m_EngineQuery, player);

			if (engine == default)
				return;

			var comboState = EntityManager.GetComponentData<GameCombo.State>(engine);
			var comboSettings = EntityManager.GetComponentData<GameCombo.Settings>(engine);
			
			if (EntityManager.TryGetComponentData(engine, out RhythmSummonEnergy energy)
			    && EntityManager.TryGetComponentData(engine, out RhythmSummonEnergyMax energyMax))
			{
				SummonEnergyReal = real(energy.Value, energyMax.MaxValue);
			}

			ComboScoreReal   = comboState.Score;
			ComboCount       = comboState.Count;
			IsFever          = comboSettings.CanEnterFever(comboState.Count, comboState.Score);

			var state  = EntityManager.GetComponentData<RhythmEngineLocalState>(engine);
			var settings = EntityManager.GetComponentData<RhythmEngineSettings>(engine);
			Pulsation = real((int)(state.Elapsed.Ticks % settings.BeatInterval.Ticks), (int)settings.BeatInterval.Ticks);

			InterpolatedEnergyReal = Mathf.MoveTowards(math.lerp(InterpolatedEnergyReal, SummonEnergyReal, Time.DeltaTime), SummonEnergyReal, Time.DeltaTime * 0.25f);

			var score = Math.Max((int) comboState.Score + Math.Max(comboState.Score >= 1.9 ? 1 + comboState.Count : 0, 0), comboState.Count);
			if (score < 0)
				score = 0;
			if (score != m_PreviousScore)
			{
				m_PreviousScoreInterpol = m_PreviousScore;
				m_PreviousScore         = score;
			}

			m_PreviousScoreInterpol = Mathf.MoveTowards(math.lerp(m_PreviousScoreInterpol, score, Time.DeltaTime * 5), score, Time.DeltaTime);
		}

		protected override void Render(FeverWormBackend backend)
		{
			if (backend.Presentation == null)
				return;
			
			var definition = backend.Presentation;
			backend.SetEnabled(ComboCount >= 2);

			definition.Animator.enabled = false;
			definition.Animator.SetFloat(_NtDoubleBeat, Pulsation);
			definition.Animator.SetFloat(_NtOneBeat, Pulsation);
			definition.Animator.SetFloat(_Score, m_PreviousScoreInterpol);
			definition.Animator.SetBool(_IsFever, IsFever);
			if (ComboCount >= 2)
				definition.Animator.Update(Time.DeltaTime);

			definition.SetStrings(ComboString, FeverString);
			definition.SetProgression(ComboScoreReal, ComboCount, InterpolatedEnergyReal, SummonEnergyReal, IsFever);
			definition.SetColors(definition.currentPulse);
		}

		protected override void ClearValues()
		{
			// not used
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			
			m_EngineQuery    = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));
		}
	}
	
	public class FeverWormCreate : PoolingSystem<FeverWormBackend, FeverWormPresentation>
	{
		private            Canvas m_Canvas;
		protected override string AddressableAsset => "core://Client/Interface/InGame/RhythmEngine/FeverWorm/FeverWorm.prefab";

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(RhythmEngineDescription), typeof(IsSimulationOwned));
		}

		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
			{
				var interfaceOrder = World.GetExistingSystem<FeverWormOrderingSystem>().Order;
				var canvasSystem   = World.GetExistingSystem<ClientCanvasSystem>();

				m_Canvas                = canvasSystem.CreateCanvas(out _, "FeverWormCanvas", defaultAddRaycaster: false);
				m_Canvas.renderMode     = RenderMode.ScreenSpaceCamera;
				m_Canvas.worldCamera    = World.GetExistingSystem<ClientCreateCameraSystem>().Camera;
				m_Canvas.planeDistance  = 1;
				m_Canvas.sortingLayerID = SortingLayer.NameToID("OverlayUI");
				m_Canvas.sortingOrder   = interfaceOrder;

				var scaler = m_Canvas.GetComponent<CanvasScaler>();
				scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1080);
				scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				scaler.matchWidthOrHeight  = 1;
			}

			base.SpawnBackend(target);

			var backend = LastBackend;
			backend.transform.SetParent(m_Canvas.transform, false);
			if (!backend.TryGetComponent(out RectTransform rt)) rt = backend.gameObject.AddComponent<RectTransform>();

			rt.localScale       = new Vector3(70, 70, 1);
			rt.anchorMin        = new Vector2(0, 0.5f);
			rt.anchorMax        = new Vector2(0, 0.5f);
			rt.anchoredPosition = new Vector2(0, 250);
			rt.sizeDelta        = new Vector2(100, 100);
			rt.pivot            = new Vector2(0.5f, 0.5f);
		}
	}
}