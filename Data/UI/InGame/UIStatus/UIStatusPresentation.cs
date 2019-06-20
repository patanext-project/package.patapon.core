using System;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.UI
{
	public class UIStatusBackend : CustomAsyncAsset<UIStatusPresentation>
	{
		public Entity entity;
		public int    position;
		
		public RectTransform rectTransform { get; private set; }

		private void OnEnable()
		{
			rectTransform = GetComponent<RectTransform>();
		}
	}

	public class UIStatusPresentation : CustomAsyncAssetPresentation<UIStatusPresentation>
	{
		public Image GaugeBackground;
		public Image RenderBackground;

		public RawImage RenderImage;
		public Image    HealthGauge;

		public override void OnBackendSet()
		{
			GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
		}

		public void SetBackgroundColor(Color color)
		{
			GaugeBackground.color  = color;
			RenderBackground.color = color;
		}

		public override void OnReset()
		{
			base.OnReset();
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(UIStatusGenerateListSystem))]
	public class UIStatusPresentationSystem : UIGameSystemBase
	{
		private EntityQuery m_Query;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Query = GetEntityQuery(typeof(UIStatusBackend), typeof(UIStatusPresentation));
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((UIStatusBackend backend) =>
			{
				var rectTransform = backend.rectTransform;
				rectTransform.anchorMin        = new Vector2(0, 0.5f);
				rectTransform.anchorMax        = new Vector2(0, 0.5f);
				rectTransform.pivot            = new Vector2(0, 0.5f);
				rectTransform.sizeDelta        = new Vector2(100, 100);
				rectTransform.anchoredPosition = new Vector2(backend.position * 100, 0);
			});
		}
	}
}