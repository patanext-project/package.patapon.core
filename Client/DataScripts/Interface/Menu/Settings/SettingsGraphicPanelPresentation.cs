using Cysharp.Threading.Tasks;
using PataNext.Client.Asset;
using PataNext.Client.DataScripts.Interface.Menu.Screens;
using PataNext.Client.Rules;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace PataNext.Client.DataScripts.Interface.Menu.Settings
{
	public class SettingsGraphicPanelPresentation : SettingsPanelPresentationBase
	{
		[SerializeField] private Slider renderScaleSlider;
		[SerializeField] private Slider msaaSlider;
		[SerializeField] private Toggle vsyncToggle;

		public override void OnBackendSet()
		{
			base.OnBackendSet();

			var ruleSystem = Backend.DstEntityManager.World
			                        .GetExistingSystem<P4GraphicsRuleSystem>();

			renderScaleSlider.SetValueWithoutNotify(math.remap(0.5f, 2f, renderScaleSlider.minValue, renderScaleSlider.maxValue, ruleSystem.RenderScale.Value));
			msaaSlider.SetValueWithoutNotify(math.remap(1, 4, msaaSlider.minValue, msaaSlider.maxValue, ruleSystem.MsaaCount.Value));
			vsyncToggle.SetIsOnWithoutNotify(ruleSystem.Vsync.Value);

			renderScaleSlider.onValueChanged.AddListener(value =>
			{
				value = math.clamp(value, renderScaleSlider.minValue, renderScaleSlider.maxValue);

				ruleSystem.RenderScale.Value = math.remap(renderScaleSlider.minValue, renderScaleSlider.maxValue, 0.5f, 2f, value);
			});

			msaaSlider.onValueChanged.AddListener(value =>
			{
				value = math.clamp(value, msaaSlider.minValue, msaaSlider.maxValue);

				ruleSystem.MsaaCount.Value = Mathf.RoundToInt(math.remap(msaaSlider.minValue, msaaSlider.maxValue, 1, 4, value));
			});
			
			vsyncToggle.onValueChanged.AddListener(value =>
			{
				ruleSystem.Vsync.Value = value;
			});
		}

		protected override void OnDataUpdate(Data data)
		{

		}

		public override string Translation => "GraphicsCategory";
		public override string Name        => "Graphics";
	}
}