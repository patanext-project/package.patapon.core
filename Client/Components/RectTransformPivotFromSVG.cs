using UnityEngine;

namespace PataNext.Client.Components
{
	[RequireComponent(typeof(RectTransform))]
	[RequireComponent(typeof(SVGImage))]
	[ExecuteAlways]
	public class RectTransformPivotFromSVG : MonoBehaviour
	{
		private SVGImage      m_Image;
		private RectTransform m_RectTransform;

		private void OnEnable()
		{
			m_RectTransform = GetComponent<RectTransform>();
			m_Image         = GetComponent<SVGImage>();
		}

		private void Update()
		{
			var size         = m_Image.sprite.rect.size;
			var pixelPivot   = m_Image.sprite.pivot;
			var percentPivot = new Vector2(pixelPivot.x / size.x, pixelPivot.y / size.y);

			if (float.IsNaN(percentPivot.x) || float.IsNaN(percentPivot.y)
			                                || float.IsInfinity(percentPivot.x) || float.IsInfinity(percentPivot.y))
			{
				Debug.LogError($"NaN or Infinity error for RectTransformPivotFromSprite({gameObject.name}).\nSize: {size}\nPixel pivot: {pixelPivot}\nPercent pivot: {percentPivot}");
				return;
			}

			m_RectTransform.pivot = percentPivot;
		}

		private void OnDisable()
		{
			m_RectTransform = null;
			m_Image         = null;
		}
	}
}