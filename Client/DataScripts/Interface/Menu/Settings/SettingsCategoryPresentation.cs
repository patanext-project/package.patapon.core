using System;
using Cysharp.Threading.Tasks;
using PataNext.Client.Asset;
using PataNext.Client.Behaviors;
using PataNext.Client.DataScripts.Interface.Menu.Screens;
using StormiumTeam.GameBase.Utility.Misc;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using StormiumTeam.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.Menu.Settings
{
	public struct SettingsCategoryData
	{
		public string TranslationId;
		public string DisplayedName;

		public AssetPath Panel;

		public SettingsCategoryData(AssetPath panel)
		{
			TranslationId = null;
			DisplayedName = null;

			Panel = panel;
		}
	}

	public class SettingsCategoryPresentation : UIPresentation<SettingsCategoryData>
	{
		[SerializeField] private TextMeshProUGUI nameLabel;
		[SerializeField] private Button          selectButton;

		private IContainer<SettingsPanelPresentationBase> panelContainer;

		[NonSerialized]
		public bool HasReceivedSelectionEvent;

		public string DisplayedName, TranslationId;

		public SettingsPanelPresentationBase CurrentPanel { get; private set; }

		public override void OnBackendSet()
		{
			base.OnBackendSet();

			OnClick(selectButton, () => { HasReceivedSelectionEvent = true; });
		}

		private AssetPath previousPanelPath;

		protected override void OnDataUpdate(SettingsCategoryData data)
		{
			if (CurrentPanel == null)
			{
				DisplayedName = data.DisplayedName;
				TranslationId = data.TranslationId;
			}

			OnDisplayedNameUpdate();

			if (data.Panel != previousPanelPath)
			{
				previousPanelPath = data.Panel;
				panelContainer?.Dispose();
				panelContainer = ContainerPool.FromPresentation<SettingsPanelPresentationBase.BackendBase, SettingsPanelPresentationBase>(
					data.Panel
				);
				panelContainer.Add().ContinueWith(args =>
				{
					var (element, _) = args;
					
					CurrentPanel  = element;
					DisplayedName = CurrentPanel.Name;
					TranslationId = CurrentPanel.Translation;
					
					OnDisplayedNameUpdate();
				});
			}
		}

		public void OnDisplayedNameUpdate()
		{
			nameLabel.text = DisplayedName ?? TranslationId ?? "Loading";
		}

		public override void OnReset()
		{
			base.OnReset();

			previousPanelPath = default;
			panelContainer?.Dispose();
		}

		public class Backend : UIBackend<SettingsCategoryData, SettingsCategoryPresentation>
		{}
		
		public class RenderSystem : BaseRenderSystem<SettingsCategoryPresentation>
		{
			private Localization localDb;

			protected override void PrepareValues()
			{
				localDb ??= World.GetExistingSystem<LocalizationSystem>()
				                 .LoadLocal("menu_screen_settings");
			}

			protected override void Render(SettingsCategoryPresentation definition)
			{
				if (definition.TranslationId == null)
					return;

				var translatedString = localDb[definition.TranslationId];
				if (definition.DisplayedName != translatedString)
				{
					definition.DisplayedName = translatedString;
					definition.OnDisplayedNameUpdate();
				}
			}

			protected override void ClearValues()
			{
			}
		}
	}
}