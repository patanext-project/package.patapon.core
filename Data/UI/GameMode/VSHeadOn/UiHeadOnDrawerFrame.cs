using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.UI.GameMode.VSHeadOn
{
	public class UiHeadOnDrawerFrame : MonoBehaviour
	{
		[Serializable]
		public struct TeamFlagSide
		{
			public MaskableGraphic circleGraphic;
		}
		
		public RectTransform Drawer;
		public RectTransform StructureFrame;
		public RectTransform UnitStatusFrame;

		public TeamFlagSide[] FlagSides;

		private Vector3[] m_Corners;

		public Vector3 BottomLeft  => m_Corners[0];
		public Vector3 TopLeft     => m_Corners[1];
		public Vector3 TopRight    => m_Corners[2];
		public Vector3 BottomRight => m_Corners[3];

		private bool m_Enabled;
		private void OnEnable()
		{
			m_Enabled = true;

			Debug.Assert(FlagSides.Length == 2, "FlagSides == 2");
			
			m_Corners = new Vector3[4];
			Drawer.GetLocalCorners(m_Corners);
		}
		
		public Vector3 GetPosition(float t)
		{
			if (!m_Enabled)
				OnEnable();
			
			var left  = Vector3.Lerp(BottomLeft, TopLeft, 0.5f);
			var right = Vector3.Lerp(BottomRight, TopRight, 0.5f);

			return math.lerp(left, right, t);
		}
	}
}