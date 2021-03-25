using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.Graphics.UI
{
	public class UITrianglePoint : UIPrimitiveBase
	{
		public Vector2 PointA;
		public Vector2 PointB;
		public Vector2 PointC;

		private List<Color32> m_colors = new List<Color32>();

		public virtual float minWidth => 0;

		public virtual float preferredWidth => 0;

		public virtual float flexibleWidth => -1;

		public virtual float minHeight => 0;

		public virtual float preferredHeight => 0;

		public virtual float flexibleHeight => -1;

		public virtual int layoutPriority => 0;

		public virtual void CalculateLayoutInputHorizontal()
		{
		}

		public virtual void CalculateLayoutInputVertical()
		{
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			vh.AddVert(PointA, color, new Vector2(0, 0));
			vh.AddVert(PointB, color, new Vector2(0, 1));
			vh.AddVert(PointC, color, new Vector2(1, 1));

			vh.AddTriangle(0, 1, 2);
		}

		public bool IsRaycastLocationValid(Vector2 sp, UnityEngine.Camera eventCamera)
		{
			return true;
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			SetAllDirty();
		}
#endif

		private void Update()
		{
			SetVerticesDirty();
		}
	}
}