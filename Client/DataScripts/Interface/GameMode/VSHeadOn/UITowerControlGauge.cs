using UnityEngine;
using UnityEngine.UI;

namespace DataScripts.Interface.GameMode.VSHeadOn
{
	public class UITowerControlGauge : MonoBehaviour
	{
		public Image         GaugeQuad;
		public RectTransform Chevron;

		private int   count;
		private float m_DelayBeforeChevronScaleUpdate;

		public Graphic[] teamQuads;

		public void SetProgression(float progression)
		{
			GaugeQuad.transform.localScale = new Vector3 {x = progression, y = 1, z = 1};
		}

		private void LateUpdate()
		{
			if (m_DelayBeforeChevronScaleUpdate < 0 || m_DelayBeforeChevronScaleUpdate > Time.time)
				return;

			Chevron.localScale              = Vector3.one * count;
			m_DelayBeforeChevronScaleUpdate = -1;
		}

		public void SetCapturingCount(int count)
		{
			if (this.count == count)
				return;

			if (count == 0)
				Chevron.localScale = Vector3.zero;
			else
				Chevron.localScale = Vector3.one;

			m_DelayBeforeChevronScaleUpdate = Time.time + 0.25f;
		}

		public void SetColor(Color color)
		{
			foreach (var quad in teamQuads)
				quad.color = color;
		}
	}
}