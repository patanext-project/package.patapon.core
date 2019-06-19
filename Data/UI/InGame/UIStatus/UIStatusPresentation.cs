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
	}

	public class UIStatusPresentation : CustomAsyncAssetPresentation<UIStatusPresentation>
	{
		public Image GaugeBackground;
		public Image RenderBackground;

		public RawImage RenderImage;
		public Image    HealthGauge;

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
			Entities.ForEach((UIStatusBackend backend, UIStatusPresentation presentation) => { });
		}
	}
}