using System;
using GameHost.ShareSimuWorldFeature.Systems;
using PataNext.Client.OrderSystems;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.BaseSystems.Ext;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Utility.AssetBackend;
using StormiumTeam.GameBase.Utility.DOTS;
using StormiumTeam.GameBase.Utility.Pooling.BaseSystems;
using StormiumTeam.GameBase.Utility.Rendering;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using StormiumTeam.GameBase.Utility.uGUI.Systems;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Random = Unity.Mathematics.Random;

namespace PataNext.Client.RhythmEngine
{
public class RhythmEngineBeatFramePresentation : RuntimeAssetPresentation<RhythmEngineBeatFramePresentation>
	{
		public GameObject[] lines = new GameObject[3];

		private int[]    m_LinesState;
		public  Material material;

		public void SetEnabled(int index, bool state)
		{
			ref var currState = ref m_LinesState[index];
			if (currState == -1)
			{
				currState = state ? 1 : 0;
				lines[index].SetActive(state);
				return;
			}

			var stateAsInt = state ? 1 : 0;
			if (currState == stateAsInt)
				return;

			currState = stateAsInt;
			lines[index].SetActive(state);
		}

		public override void OnBackendSet()
		{
			m_LinesState = new int[lines.Length];
			foreach (ref var state in m_LinesState.AsSpan()) state = -1;
		}
	}

	public class RhythmEngineBeatFrameBackend : RuntimeAssetBackend<RhythmEngineBeatFramePresentation>
	{
		public enum Phase
		{
			NoCommand,
			Fever,
			Command
		}

		public override bool PresentationWorldTransformStayOnSpawn => false;
	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class RhythmEngineBeatFrameRenderSystem : BaseRenderSystem<RhythmEngineBeatFramePresentation>
	{
		private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

		public float BeatTime;
		public float BeatTimeExp;

		public int CurrentHue;

		private EntityQuery                        m_EngineQuery;
		public  RhythmEngineBeatFrameBackend.Phase PreviousPhase;

		public Color TargetColor;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_EngineQuery = GetEntityQuery(typeof(RhythmEngineDescription), typeof(Relative<PlayerDescription>));
		}

		private static void SetGrayScale(ref Color c, float v)
		{
			c.r = v;
			c.g = v;
			c.b = v;
		}

		protected override void PrepareValues()
		{
			var player = this.GetFirstSelfGamePlayer();
			if (player == default)
				return;

			var interFrame = GetSingleton<InterFrame>();
			
			Entity engine;
			
			var cameraState = this.GetComputedCameraState().StateData;
			if (cameraState.Target != default)
				engine = PlayerComponentFinder.GetRelativeChild<RhythmEngineDescription>(EntityManager, m_EngineQuery, cameraState.Target, player);
			else
				engine = PlayerComponentFinder.FromQueryFindPlayerChild(m_EngineQuery, player);

			if (engine == default)
				return;

			var process            = EntityManager.GetComponentData<RhythmEngineLocalState>(engine);
			var currentCommand     = EntityManager.GetComponentData<RhythmEngineExecutingCommand>(engine);
			var serverCommandState = EntityManager.GetComponentData<GameCommandState>(engine);
			var comboSettings = EntityManager.GetComponentData<GameCombo.Settings>(engine);
			var comboState = EntityManager.GetComponentData<GameCombo.State>(engine);

			var isCommand = serverCommandState.StartTimeMs <= process.Elapsed.TotalMilliseconds 
			                && serverCommandState.EndTimeMs > process.Elapsed.TotalMilliseconds;
			
			if (interFrame.Range.Contains(process.NewBeatTick))
			{
				BeatTime    = 0;
				BeatTimeExp = 0;
			}
			else
			{
				if (PreviousPhase != RhythmEngineBeatFrameBackend.Phase.Fever)
					BeatTime += Time.DeltaTime * 2f + BeatTimeExp * 0.5f;
				else
					BeatTime += Time.DeltaTime * 2f;

				BeatTimeExp += Time.DeltaTime;
			}

			TargetColor.a = 1.0f;
			if (interFrame.Range.Contains(process.NewBeatTick))
			{
				if (comboSettings.CanEnterFever(comboState.Count, comboState.Score))
				{
					PreviousPhase =  RhythmEngineBeatFrameBackend.Phase.Fever;
					CurrentHue    += 1;
				}
				else
				{
					PreviousPhase = RhythmEngineBeatFrameBackend.Phase.NoCommand;
				}
			}

			if (isCommand && interFrame.Range.Contains(process.NewBeatTick)) PreviousPhase = RhythmEngineBeatFrameBackend.Phase.Command;

			switch (PreviousPhase)
			{
				case RhythmEngineBeatFrameBackend.Phase.NoCommand:
				{
					SetGrayScale(ref TargetColor, 0.75f);
					break;
				}

				case RhythmEngineBeatFrameBackend.Phase.Command:
				{
					SetGrayScale(ref TargetColor, 0.5f);
					break;
				}

				case RhythmEngineBeatFrameBackend.Phase.Fever:
				{
					// goooo crazy // TODO: Implement JinnEnergy
					if (true/* comboState.JinnEnergy < comboState.JinnEnergyMax*/)
					{
						for (var i = 0; i != 3; i++) TargetColor[i] = Mathf.Lerp(TargetColor[i], new Random((uint) Environment.TickCount).NextFloat(), Time.DeltaTime * 25f);

						TargetColor[CurrentHue % 3] = 1;
					}
					// this color should be customizable but i'm lazy to add that settings so... eh...
					/*else
					{
						TargetColor = new Color(1, 0.86f, 0, 1);
					}*/

					break;
				}
			}
		}

		protected override void Render(RhythmEngineBeatFramePresentation definition)
		{
			switch (PreviousPhase)
			{
				case RhythmEngineBeatFrameBackend.Phase.NoCommand:
				{
					definition.SetEnabled(0, false);
					definition.SetEnabled(1, true);
					definition.SetEnabled(2, false);
					break;
				}

				case RhythmEngineBeatFrameBackend.Phase.Command:
				{
					definition.SetEnabled(0, true);
					definition.SetEnabled(1, false);
					definition.SetEnabled(2, true);
					break;
				}

				case RhythmEngineBeatFrameBackend.Phase.Fever:
				{
					definition.SetEnabled(0, true);
					definition.SetEnabled(1, true);
					definition.SetEnabled(2, true);
					break;
				}
			}

			var noAlphaColor = TargetColor;
			noAlphaColor.a = 0.0f;

			var lerpResult = Color.Lerp(TargetColor, noAlphaColor, BeatTime);
			definition.material.SetColor(ColorId, lerpResult);
		}

		protected override void ClearValues()
		{
		}
	}
	
	public class RhythmEngineBeatFrameCreate : PoolingSystem<RhythmEngineBeatFrameBackend, RhythmEngineBeatFramePresentation>
	{		
		private            Canvas m_Canvas;
		protected override string AddressableAsset => "core://Client/Interface/InGame/RhythmEngine/BeatEffect.prefab";

		protected override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(RhythmEngineDescription), typeof(IsSimulationOwned));
		}

		protected override void SpawnBackend(Entity target)
		{
			if (m_Canvas == null)
			{
				var interfaceOrder = World.GetExistingSystem<RhythmEngineBeatFrameOrdering>().Order;
				var canvasSystem   = World.GetExistingSystem<ClientCanvasSystem>();

				m_Canvas                  = canvasSystem.CreateCanvas(out _, "BeatFrameCanvas");
				m_Canvas.renderMode       = RenderMode.ScreenSpaceCamera;
				m_Canvas.worldCamera      = World.GetExistingSystem<ClientCreateCameraSystem>().Camera;
				m_Canvas.planeDistance    = 1;
				m_Canvas.sortingLayerName = "OverlayUI";
				m_Canvas.sortingOrder     = interfaceOrder;

				var scaler = m_Canvas.GetComponent<CanvasScaler>();
				scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1080);
				scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				scaler.matchWidthOrHeight  = 0.5f;
			}

			base.SpawnBackend(target);

			var backend = LastBackend;
			backend.transform.SetParent(m_Canvas.transform, false);
			if (!backend.TryGetComponent(out RectTransform rt)) rt = backend.gameObject.AddComponent<RectTransform>();

			rt.localScale       = new Vector3(1, 1, 1);
			rt.anchorMin        = new Vector2(0, 0);
			rt.anchorMax        = new Vector2(1, 1);
			rt.anchoredPosition = new Vector2(0, 0);
			rt.sizeDelta        = new Vector2(0, 0);
			rt.pivot            = new Vector2(0.5f, 0.5f);
		}
	}
}