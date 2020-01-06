using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.GameModes.Interface
{
	public class UIHeadOnDrawerFrame : MonoBehaviour
	{
		[Serializable]
		public struct TeamFlagSide
		{
			public MaskableGraphic circleGraphic;
		}
		
		public RectTransform Drawer;
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
		
		public Vector3 GetPosition(float t, DrawerAlignment alignment)
		{
			if (!m_Enabled)
				OnEnable();

			Vector3 left, right;
			switch (alignment)
			{
				case DrawerAlignment.Top:
					left = TopLeft;
					right = TopRight;
					break;
				case DrawerAlignment.Center:
					left = Vector3.Lerp(BottomLeft, TopLeft, 0.5f);
					right = Vector3.Lerp(BottomRight, TopRight, 0.5f);
					break;
				case DrawerAlignment.Bottom:
					left = BottomLeft;
					right = BottomRight;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
			}
	/*		
			var left  = Vector3.Lerp(BottomLeft, TopLeft, 0.5f);
			var right = Vector3.Lerp(BottomRight, TopRight, 0.5f);
*/
			return math.lerp(left, right, t);
		}
	}
}