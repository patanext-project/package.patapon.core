using StormiumTeam.GameBase;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.GameModes.Interface
{
	public class UIHeadOnUnitStatusPresentation : RuntimeAssetPresentation<UIHeadOnUnitStatusPresentation>
	{
		public Image           HealthGauge;
		public MaskableGraphic TeamCircleQuad;
		public MaskableGraphic PossessionOutlineQuad;
		public Gradient        Gradient;

		public override void OnBackendSet()
		{
			GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
		}

		public void SetHealth(int value, int max)
		{
			var real = 0.0f;
			if (value > 0 && max > 0)
			{
				real = value / (float) max;
			}

			HealthGauge.fillAmount = real;
			HealthGauge.color      = Gradient.Evaluate(real);
		}

		public void SetTeamColor(Color color)
		{
			TeamCircleQuad.color = color;
		}

		public void SetPossessionColor(Color color)
		{
			PossessionOutlineQuad.color = color;
		}

		public override void OnReset()
		{
			base.OnReset();
		}
	}

	public class UIHeadOnUnitStatusBackend : RuntimeAssetBackend<UIHeadOnUnitStatusPresentation>
	{
		public UIHeadOnUnitStatusPresentation Hud;

		private RectTransform m_RectTransform;
		public  RectTransform rectTransform => m_RectTransform;

		public override void OnTargetUpdate()
		{
			DstEntityManager.AddComponentData(BackendEntity, RuntimeAssetDisable.All);
		}

		public override void OnComponentEnabled()
		{
			if (!gameObject.TryGetComponent(out m_RectTransform))
				m_RectTransform = gameObject.AddComponent<RectTransform>();
		}
	}
}