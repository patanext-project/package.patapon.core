using Misc;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Patapon.Mixed.Rules
{
	public class P4NetworkRules : RuleBaseSystem
	{
		public enum Interpolation
		{
			Default,
			Double
		}

		public BaseRuleConfiguration RuleConfiguration;
		public RuleProperties<Data>.Property<bool> RhythmEngineUsePredicted;

		public RuleProperties<Data>                         Rule;
		public RuleProperties<Data>.Property<Interpolation> UnitPresentationInterpolation;

		protected override void OnCreate()
		{
			base.OnCreate();

			Rule                          = AddRule<Data>();
			RhythmEngineUsePredicted      = Rule.Add(null, data => data.RhythmEngineUsePredicted);
			UnitPresentationInterpolation = Rule.Add(null, data => data.UnitPresentationInterpolation);

			RhythmEngineUsePredicted.Value      = true;
			UnitPresentationInterpolation.Value = Interpolation.Double;
			
			RuleConfiguration = new BaseRuleConfiguration(Rule, this, 1);
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return inputDeps;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			
			RuleConfiguration.SaveAndDispose();
		}

		public struct Data : IComponentData
		{
			public bool          RhythmEngineUsePredicted;
			public Interpolation UnitPresentationInterpolation;
		}
	}
}