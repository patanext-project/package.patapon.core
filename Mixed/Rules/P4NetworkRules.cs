using Misc;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Patapon.Mixed.Rules
{
	public class P4NetworkRules : RuleBaseSystem<P4NetworkRules.Data>
	{
		public enum Interpolation
		{
			Default,
			DoubleInterpolated,
			DoublePredicted
		}

		public RuleProperties<Data>.Property<bool>          RhythmEngineUsePredicted;
		public RuleProperties<Data>.Property<Interpolation> UnitPresentationInterpolation;
		public RuleProperties<Data>.Property<bool>          AbilityUsePredicted;

		protected override void AddRuleProperties()
		{
			RhythmEngineUsePredicted      = Rule.Add(null, data => data.RhythmEngineUsePredicted);
			UnitPresentationInterpolation = Rule.Add(null, data => data.UnitPresentationInterpolation);
			AbilityUsePredicted           = Rule.Add(null, data => data.AbilityUsePredicted);
		}

		protected override void SetDefaultProperties()
		{
			RhythmEngineUsePredicted.Value      = false;
			UnitPresentationInterpolation.Value = Interpolation.DoubleInterpolated;
			AbilityUsePredicted.Value           = false;
		}

		public struct Data : IComponentData
		{
			public bool          RhythmEngineUsePredicted;
			public Interpolation UnitPresentationInterpolation;
			public bool          AbilityUsePredicted;
		}
	}
}