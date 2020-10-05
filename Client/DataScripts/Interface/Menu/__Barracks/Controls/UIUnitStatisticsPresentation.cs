using System;
using StormiumTeam.GameBase.Utility.AssetBackend;
using TMPro;
using UnityEngine;

namespace PataNext.Client.DataScripts.Interface.Menu.__Barracks.Controls
{
	public class UIUnitStatisticsPresentation : RuntimeAssetPresentation<UIUnitStatisticsPresentation>
	{
		[SerializeField]
		private GameObject labelToInstantiate;

		[SerializeField]
		private RectTransform instanceRoot;
		
		private TextMeshProUGUI[] spawnedLabels;

		private string[][] m_PageData;
		private bool                             m_CurrentPageDataIsDirty;

		private void OnEnable()
		{
			spawnedLabels = Array.Empty<TextMeshProUGUI>();
			
			m_PageData = Array.Empty<string[]>();

			ClearSpawnedLabels();

			// todo Temporary
			SetData(0, new[] {"XP/Next<pos=35%>726/1000", "Health<color=#e6342eff><pos=32%><size=90%><b>+</b></size><pos=35%>180", "Attk. Speed<pos=35%>2.0"});
			CurrentPage = 0;
		}

		private void ClearSpawnedLabels()
		{
			if (spawnedLabels != null)
				foreach (var label in spawnedLabels)
				{
					Destroy(label.gameObject);
				}

			spawnedLabels = Array.Empty<TextMeshProUGUI>();
		}

		public void SetData(int page, string[] rowData)
		{
			if (page >= m_PageData.Length)
				Array.Resize(ref m_PageData, page + 1);
			m_PageData[page] = rowData;

			if (page == CurrentPage)
				m_CurrentPageDataIsDirty = true;
		}

		private int m_CurrentPage;
		public int CurrentPage
		{
			get
			{
				return m_CurrentPage;
			}
			set
			{
				var updateData = false;
				if (m_CurrentPage != value || m_CurrentPageDataIsDirty)
				{
					m_CurrentPageDataIsDirty = false;
					updateData               = true;
				}
				
				m_CurrentPage = value;

				if (updateData)
				{
					var rowData = m_PageData[m_CurrentPage];

					if (spawnedLabels.Length != rowData.Length)
					{
						ClearSpawnedLabels();
						spawnedLabels = new TextMeshProUGUI[rowData.Length];
						for (var i = 0; i != spawnedLabels.Length; i++)
						{
							spawnedLabels[i]      = Instantiate(labelToInstantiate, instanceRoot).GetComponent<TextMeshProUGUI>();
							spawnedLabels[i].text = string.Empty;
						}
					}

					for (var i = 0; i != spawnedLabels.Length; i++)
					{
						spawnedLabels[i].text = rowData[i];
					}
				}
			}
		}

		public int PageCount { get; set; }
	}
}