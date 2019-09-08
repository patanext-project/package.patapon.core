using System;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.Shared.Gen;
using Unity.Collections;
using Unity.Entities;
using Revolution.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.UI
{
	public class UIStatusBackend : RuntimeAssetBackend<UIStatusPresentation>
	{
		public int priority;

		public RectTransform rectTransform { get; private set; }

		public override void OnTargetUpdate()
		{
			DstEntityManager.AddComponentData(BackendEntity, RuntimeAssetDisable.All);
		}
		
		public override void OnComponentEnabled()
		{
			rectTransform = GetComponent<RectTransform>();
		}
	}

	public class UIStatusPresentation : RuntimeAssetPresentation<UIStatusPresentation>
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
		private struct SortBackend : IComparable<SortBackend>
		{
			public int index;
			public int priority;

			public Entity entity;

			public int CompareTo(SortBackend other)
			{
				return other.priority - priority;
			}
		}

		private EntityQuery m_Query;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Query = GetEntityQuery(typeof(UIStatusBackend));
		}

		protected override void OnUpdate()
		{
			var length = m_Query.CalculateEntityCount();
			var sorted = new NativeArray<SortBackend>(length, Allocator.Temp);

			UIStatusBackend backend = null;
			foreach (var (i, entity) in this.ToEnumerator_C(m_Query, ref backend))
			{
				sorted[i] = new SortBackend {index = i, priority = backend.priority, entity = entity};
			}

			for (var i = 0; i != sorted.Length; i++)
			{
				backend = EntityManager.GetComponentObject<UIStatusBackend>(sorted[i].entity);

				var rectTransform = backend.rectTransform;
				rectTransform.anchorMin        = new Vector2(0, 0.5f);
				rectTransform.anchorMax        = new Vector2(0, 0.5f);
				rectTransform.pivot            = new Vector2(0, 0.5f);
				rectTransform.sizeDelta        = new Vector2(100, 100);
				rectTransform.anchoredPosition = new Vector2(i * 100, 0);
			}

			foreach (var enumeration in this.ToEnumerator_C(m_Query, ref backend))
			{
				// normal behavior here...
			}
		}
	}
}