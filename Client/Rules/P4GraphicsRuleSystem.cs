using System.ComponentModel;
using System.Data;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Utility.Rules;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DefaultNamespace
{
	public struct P4GraphicsRule : IComponentData
	{
		public float RenderScale;
		public int   MsaaCount;
	}

	public class P4GraphicsRuleSystem : RuleBaseSystem<P4GraphicsRule>
	{
		public RuleProperties<P4GraphicsRule>.Property<float> RenderScale;
		public RuleProperties<P4GraphicsRule>.Property<int>   MsaaCount;

		protected override void AddRuleProperties()
		{
			RenderScale = Rule.Add(d => d.RenderScale);
			MsaaCount   = Rule.Add(d => d.MsaaCount);

			RenderScale.OnVerify += (ref float value) =>
			{
				value = math.clamp(value, 0.1f, 2);
				return true;
			};
			MsaaCount.OnVerify += (ref int value) =>
			{
				value = math.clamp(value, 0, 4);
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
		}

		protected override void SetDefaultProperties()
		{
			RenderScale.Value = 2;
			MsaaCount.Value   = 4;
		}
	}
}