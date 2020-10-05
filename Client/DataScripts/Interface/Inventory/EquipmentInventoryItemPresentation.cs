using System;
using StormiumTeam.GameBase.Utility.AssetBackend;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.Inventory
{
	public class EquipmentInventoryItemPresentation : RuntimeAssetPresentation<EquipmentInventoryItemPresentation>
	{
		public SVGImage   graphic;
		public SVGImage   background;
		public GameObject selected;
		public GameObject equipped;

		public void SetSprite(Sprite sprite)
		{
			graphic.sprite = sprite;
		}

		private bool m_WasSelected;
		private bool m_WasEquipped;

		private void OnEnable()
		{
			m_WasSelected = m_WasEquipped = false;
			selected.SetActive(m_WasSelected);
			equipped.SetActive(m_WasEquipped);
		}

		private bool Set(bool value, ref bool target, GameObject go)
		{
			if (value == target)
				return false;

			target = value;
			go.SetActive(target);
			return true;
		}

		public void SetSelected(bool value)
		{
			Set(value, ref m_WasSelected, selected);
		}
		
		public void SetEquipped(bool value)
		{
			Set(value, ref m_WasEquipped, equipped);
		}
	}
}