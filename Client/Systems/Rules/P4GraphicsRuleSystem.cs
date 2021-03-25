using System.ComponentModel;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Utility.Rules;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PataNext.Client.Rules
{
	public struct P4GraphicsRule : IComponentData
	{
		public float RenderScale;
		public int   MsaaCount;
		public bool  Vsync;
	}

	public class P4GraphicsRuleSystem : RuleBaseSystem<P4GraphicsRule>
	{
		public RuleProperties<P4GraphicsRule>.Property<float> RenderScale;
		public RuleProperties<P4GraphicsRule>.Property<int>   MsaaCount;
		public RuleProperties<P4GraphicsRule>.Property<bool>   Vsync;

		protected override void AddRuleProperties()
		{
			RenderScale = Rule.Add(d => d.RenderScale);
			MsaaCount   = Rule.Add(d => d.MsaaCount);
			Vsync       = Rule.Add(d => d.Vsync);

			RenderScale.OnVerify += (ref float value) =>
			{
				value = math.clamp(value, 0.1f, 2);
				return true;
			};
			MsaaCount.OnVerify += (ref int value) =>
			{
				value = math.clamp(value, 1, 4);
				return true;
			};

			Rule.OnPropertyChanged += OnHandler;
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			OnHandler(null, null);
		}

		private void OnHandler(object sender, PropertyChangedEventArgs args)
		{
			var lowPipeline = (UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline;
			lowPipeline.renderScale     = RenderScale.Value;
			lowPipeline.msaaSampleCount = MsaaCount.Value;

			UnityEngine.QualitySettings.vSyncCount = Vsync.Value ? 1 : 0;
		}

		protected override void SetDefaultProperties()
		{
			RenderScale.Value = 1.25f;
			MsaaCount.Value   = 2;
			Vsync.Value       = true;
		}
	}
}