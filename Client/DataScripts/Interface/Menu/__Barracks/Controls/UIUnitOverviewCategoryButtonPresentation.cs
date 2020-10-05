using System;
using StormiumTeam.GameBase.Utility.AssetBackend;
using TMPro;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Controls
{
	public class UIUnitOverviewCategoryButtonPresentation : RuntimeAssetPresentation<UIUnitOverviewCategoryButtonPresentation>
	{
		public enum EPhase
		{
			None,
			Selected,
			Active
		}

		public SVGImage[]        iconQuad;
		public TextMeshProUGUI[] label;

		public GameObject onNoneGameObject;
		public GameObject onSelectedGameObject;
		public GameObject onActiveGameObject;

		private EPhase m_Phase;

		private void Awake()
		{
			m_Phase = EPhase.None;

			SetPhase(m_Phase, true);
		}

		public void SetName(string content)
		{
			foreach (var l in label)
				l.text = content;
		}

		public void SetIcon(Sprite sprite)
		{
			foreach (var i in iconQuad)
				i.sprite = sprite;
		}

		public void SetPhase(EPhase value, bool forced = false)
		{
			if (m_Phase == value && !forced)
				return;

			SetEnabled(onNoneGameObject, value == EPhase.None);
			SetEnabled(onSelectedGameObject, value == EPhase.Selected);
			SetEnabled(onActiveGameObject, value == EPhase.Active);

			m_Phase = value;
		}

		private void SetEnabled(GameObject go, bool value)
		{
			if (go != null)
				go.SetActive(value);
		}
	}
}