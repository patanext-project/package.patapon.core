using System;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.UI
{
	public class UIStatusBackend : RuntimeAssetBackend<UIStatusPresentation>
	{
		public Entity entity;
		public int    priority;

		public RectTransform rectTransform { get; private set; }

		private void OnEnable()
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
			using (var chunks = m_Query.CreateArchetypeChunkArray(Allocator.TempJob))
			{
				var backendType = GetArchetypeChunkComponentType<UIStatusBackend>();
				foreach (var chunk in chunks)
				{
					var backendArray = chunk.GetComponentObjects(backendType, EntityManager);
					var sorted       = new NativeArray<SortBackend>(chunk.Count, Allocator.Temp);

					// first, sort backends...
					for (var ent = 0; ent != chunk.Count; ent++)
					{
						sorted[ent] = new SortBackend {index = ent, priority = backendArray[ent].priority};
					}

					sorted.Sort();

					for (var i = 0; i != sorted.Length; i++)
					{
						var backend       = backendArray[sorted[i].index];
						var rectTransform = backend.GetComponent<RectTransform>();

						rectTransform.anchorMin        = new Vector2(0, 0.5f);
						rectTransform.anchorMax        = new Vector2(0, 0.5f);
						rectTransform.pivot            = new Vector2(0, 0.5f);
						rectTransform.sizeDelta        = new Vector2(100, 100);
						rectTransform.anchoredPosition = new Vector2(i * 100, 0);
					}
				}
			}

			Entities.ForEach((UIStatusBackend backend) =>
			{
				var rectTransform = backend.rectTransform;
			});
		}
	}
}