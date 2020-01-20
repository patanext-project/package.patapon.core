using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Patapon4TLB.GameModes.Interface
{
	public enum UIHeadOnDrawerType
	{
		Structure,
		DeadUnit,
		Enemy,
		Ally
	}

	public class UIHeadOnDrawerFrame : MonoBehaviour
	{
		[Serializable]
		public struct TeamFlagSide
		{
			public MaskableGraphic circleGraphic;
		}

		[SerializeField] private RectTransform drawerSpace;
		
		[SerializeField] private RectTransform structureDrawer;
		[SerializeField] private RectTransform deadUnitDrawer;
		[SerializeField] private RectTransform enemyDrawer;
		[SerializeField] private RectTransform allyDrawer;

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
			drawerSpace.GetLocalCorners(m_Corners);
		}

		public Transform GetDrawer(UIHeadOnDrawerType type)
		{
			switch (type)
			{
				case UIHeadOnDrawerType.Structure:
					return structureDrawer;
				case UIHeadOnDrawerType.DeadUnit:
					return deadUnitDrawer;
				case UIHeadOnDrawerType.Enemy:
					return enemyDrawer;
				case UIHeadOnDrawerType.Ally:
					return allyDrawer;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
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

			return math.lerp(left, right, t);
		}
	}
}