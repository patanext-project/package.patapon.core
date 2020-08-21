using System;
using StormiumTeam.GameBase.Utility.AssetBackend;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.Inventory
{
	public class EquipmentInventoryItemPresentation : RuntimeAssetPresentation<EquipmentInventoryItemPresentation>
	{
		public SVGImage   graphic;
		public GameObject selected;

		public void SetSprite(Sprite sprite)
		{
			graphic.sprite = sprite;
		}

		private bool m_WasSelected;

		private void OnEnable()
		{
			m_WasSelected = false;
			selected.SetActive(m_WasSelected);
		}

		public void SetSelected(bool value)
		{
			if (value == m_WasSelected) 
				return;
			
			m_WasSelected = value;
			selected.SetActive(m_WasSelected);
		}
	}
}