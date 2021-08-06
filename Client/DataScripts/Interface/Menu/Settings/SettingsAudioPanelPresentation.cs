using PataNext.Client.DataScripts.Interface.Menu.Screens;
using PataNext.Client.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.Menu.Settings
{
	public class SettingsAudioPanelPresentation : SettingsPanelPresentationBase
	{
		[SerializeField] private Toggle enableDrumToggle;

		public override void OnBackendSet()
		{
			base.OnBackendSet();
			
			var ruleSystem = Backend.DstEntityManager.World
			                        .GetExistingSystem<P4SoundRules>();
			
			enableDrumToggle.SetIsOnWithoutNotify(ruleSystem.EnableDrumVoices.Value);
			
			enableDrumToggle.onValueChanged.AddListener(isOn =>
			{
				ruleSystem.EnableDrumVoices.Value = isOn;
			});
		}

		protected override void   OnDataUpdate(Data data)
		{
			
		}

		public override string Translation => "AudioCategory";
		public override string Name        => "Audio";
	}
}