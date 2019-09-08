using System;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Revolution.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Patapon4TLB.UI.GameMode.VSHeadOn
{
	public class UiHeadOnPresentation : RuntimeAssetPresentation<UiHeadOnPresentation>
	{
		public UiHeadOnScoreFrame[] ScoreFrames;
		public UiHeadOnDrawerFrame  DrawerFrame;

		public TextMeshProUGUI ChronoLabel;

		[NonSerialized]
		public float[] FlagPositions;

		[NonSerialized]
		private int m_PreviousTime;

		private ClubInformation[] m_ClubInformationArray;
		
		private void OnEnable()
		{
			Debug.Assert(ScoreFrames.Length == 2, "ScoreFrames.Length == 2");

			m_ClubInformationArray = new ClubInformation[2];
			FlagPositions = new float[2];
			foreach (var frame in ScoreFrames)
			{
				foreach (var category in frame.Categories)
				{
					category.Label.text = "0";
				}
			}
		}

		public void SetTime(int seconds)
		{
			if (m_PreviousTime.Equals(seconds))
				return;
			m_PreviousTime = seconds;
			
			if (seconds < 0)
			{
				ChronoLabel.text = string.Empty;
				return;
			}

			var timespan = TimeSpan.FromSeconds(seconds);
			ChronoLabel.text = $"<mspace=0.46em>{timespan:mm}</mspace>:<mspace=0.46em>{timespan:ss}</mspace>";
		}

		public void SetScore(int category, int team, int score)
		{
			Debug.Assert(team >= 0 && team < ScoreFrames.Length, "team >= 0 && team < ScoreFrames.Length");
			var frame = ScoreFrames[team];
			frame.SetScore(category, score);
		}

		public void SetFlagPosition(float3 flag0Pos, float3 flag1Pos)
		{
			FlagPositions[0] = flag0Pos.x;
			FlagPositions[1] = flag1Pos.x;
		}

		public float3 GetPositionOnDrawer(float3 position, bool limit = true)
		{
			var t = math.unlerp(FlagPositions[0], FlagPositions[1], position.x);
			if (limit)
				t = math.clamp(t, 0, 1);
			
			return DrawerFrame.GetPosition(t);
		}

		public void UpdateClubInformation(ClubInformation team1, ClubInformation team2)
		{
			m_ClubInformationArray[0] = team1;
			m_ClubInformationArray[1] = team2;

			for (var index = 0; index < DrawerFrame.FlagSides.Length; index++)
			{
				var side = DrawerFrame.FlagSides[index];
				var team = m_ClubInformationArray[index];
				
				side.circleGraphic.color  = team.PrimaryColor;
			}

			for (var index = 0; index < ScoreFrames.Length; index++)
			{
				var frame = ScoreFrames[index];
				var team  = m_ClubInformationArray[index];

				foreach (var cat in frame.Categories)
				{
					cat.Image.color = team.PrimaryColor;
				}
			}
		}

		[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
		public class Manager : ComponentSystem
		{
			public const string AddrKey = "int:UI/GameModes/VSHeadOn/VSHeadOnInterface.prefab";

			public GameObject Element;

			private bool m_EnableState;
			private bool m_EnableDirty;

			private UIClientCanvasSystem m_CanvasManager;

			private Canvas m_Canvas;
			private int    m_CanvasIndex;

			public Canvas Canvas => m_Canvas;

			private EntityQuery m_CameraQuery;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_EnableDirty = true;

				m_CameraQuery = GetEntityQuery(typeof(GameCamera));
				m_CanvasManager = World.GetOrCreateSystem<UIClientCanvasSystem>();

				m_Canvas                  = m_CanvasManager.CreateCanvas(out m_CanvasIndex, "UIVersusHeadOn");
				m_Canvas.renderMode       = RenderMode.ScreenSpaceCamera;
				m_Canvas.planeDistance    = 10;
				m_Canvas.sortingOrder     = (int) UICanvasOrder.HudHeadOnInterface;
				m_Canvas.sortingLayerName = "OverlayUI";
				m_Canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal
				                                    | AdditionalCanvasShaderChannels.Tangent
				                                    | AdditionalCanvasShaderChannels.TexCoord1
				                                    | AdditionalCanvasShaderChannels.TexCoord2
				                                    | AdditionalCanvasShaderChannels.TexCoord3;
				var canvasScaler = m_Canvas.GetComponent<CanvasScaler>();
				canvasScaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				canvasScaler.referenceResolution    = new Vector2(1920, 1080);
				canvasScaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				canvasScaler.matchWidthOrHeight     = 0;
				canvasScaler.referencePixelsPerUnit = 100;

				Addressables.InstantiateAsync(AddrKey).Completed += handle =>
				{
					Element = handle.Result;
					Element.AddComponent<GameObjectEntity>();
					Element.transform.SetParent(m_Canvas.transform, false);
				};
			}

			protected override void OnUpdate()
			{
				if (m_CameraQuery.IsEmptyIgnoreFilter)
					return;
					
				m_Canvas.worldCamera = EntityManager.GetComponentObject<Camera>(m_CameraQuery.GetSingletonEntity());
				m_Canvas.sortingLayerName = "OverlayUI";

				if (m_EnableDirty && Element != null)
				{
					m_EnableDirty = false;
					Element.SetActive(m_EnableState);
				}
			}

			public void SetEnabled(bool state)
			{
				if (m_EnableState != state)
				{
					m_EnableDirty = true;
				}

				m_EnableState = state;

				if (!m_EnableDirty || Element == null)
					return;

				m_EnableDirty = false;
				Element.SetActive(m_EnableState);
			}
		}
	}
}