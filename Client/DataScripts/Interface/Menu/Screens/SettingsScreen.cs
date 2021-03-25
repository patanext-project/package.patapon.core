using System;
using Cysharp.Threading.Tasks;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Asset;
using PataNext.Client.Behaviors;
using PataNext.Client.Core.DOTSxUI.Components;
using PataNext.Client.DataScripts.Interface.Menu.Settings;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.Menu.Screens
{
	public abstract class SettingsPanelPresentationBase : UIPresentation<SettingsPanelPresentationBase.Data>
	{
		public class BackendBase : UIBackend<Data, SettingsPanelPresentationBase>
		{}
		
		public struct Data
		{
		}

		public abstract string Translation { get; }
		public abstract string Name        { get; }

		public bool IsAttached;
	}

	public struct SettingsScreenData
	{
		public AssetPath[] Panels;
	}

	public class SettingsScreen : UIPresentation<SettingsScreenData>
	{
		public class BackendBase : UIBackend<SettingsScreenData, SettingsScreen>
		{}
		
		[SerializeField] private GameObject categoryReference;
		[SerializeField] private Transform  categoryRoot;

		[SerializeField] private Transform panelRoot;
		
		[SerializeField] private Button quitScreenButton;

		private IContainer<SettingsCategoryPresentation> categoryContainer;

		private int panelIndex;

		private void Awake()
		{
			panelIndex = -1;
			categoryContainer = ContainerPool.FromPresentation<SettingsCategoryPresentation.Backend, SettingsCategoryPresentation>(categoryReference)
			                                 .WithTransformRoot(categoryRoot);
		}

		private void OnEnable()
		{
			quitScreenButton.interactable = true;
		}

		public override void OnBackendSet()
		{
			base.OnBackendSet();
			
			OnClick(quitScreenButton, () =>
			{
				// Make sure that the user don't trigger it multiple time if we are transitioning.
				quitScreenButton.interactable = false;
				
				Debug.LogError("quit");

				var entityMgr = Backend.DstEntityManager;
				if (entityMgr.HasComponent<UIScreen>(Backend.BackendEntity))
					entityMgr.AddComponent<UIScreen.WantToQuit>(Backend.BackendEntity);
				else
					Backend.Return(true, true);
			});
		}

		protected override async void OnDataUpdate(SettingsScreenData data)
		{
			await categoryContainer.Warm();

			if (data.Panels == null)
				return;

			// We know that the container is using an AsyncAssetPool in the backing, and that Warm() is sufficient to know that the rest of the calls
			// can be synchronous
			categoryContainer.SetSize(data.Panels.Length);

			var collection = categoryContainer.GetList();
			for (var i = 0; i < data.Panels.Length; i++)
			{
				collection[i].Data = new SettingsCategoryData(data.Panels[i]);
			}
		}

		public class RenderSystem : BaseRenderSystem<SettingsScreen>
		{
			protected override void PrepareValues()
			{
			}

			protected override void Render(SettingsScreen definition)
			{
				var nextPanel = -1;
				
				var list = definition.categoryContainer.GetList();
				for (var i = 0; i < list.Count; i++)
				{
					if (list[i].HasReceivedSelectionEvent)
					{
						list[i].HasReceivedSelectionEvent = false;
						nextPanel                         = i;
					}

					if (list[i].CurrentPanel is { } panel)
					{
						if (!panel.IsAttached)
						{
							panel.IsAttached = true;
							panel.gameObject.SetActive(false);
							panel.transform.SetParent(definition.panelRoot, false);
						}
					}
				}

				if (nextPanel != -1)
				{
					if (definition.panelIndex != nextPanel && definition.panelIndex >= 0)
					{
						var current = list[definition.panelIndex];
						if (current.CurrentPanel is {} panel)
							panel.gameObject.SetActive(false);
					}
					
					definition.panelIndex = nextPanel;

					var next = list[definition.panelIndex];
					if (next.CurrentPanel is { } nextPanelObj)
					{
						nextPanelObj.gameObject.SetActive(true);
					}
					else
					{
						Debug.LogError("No panel found for index: " + nextPanel);
					}
				}
			}

			protected override void ClearValues()
			{
			}
		}
	}
}