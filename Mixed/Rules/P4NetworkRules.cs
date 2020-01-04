using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using Unity.Jobs;

namespace Patapon.Mixed.Rules
{
	public class P4NetworkRules : RuleBaseSystem
	{
		public enum Interpolation
		{
			Default,
			Double
		}

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
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return inputDeps;
		}

		public struct Data : IComponentData
		{
			public bool          RhythmEngineUsePredicted;
			public Interpolation UnitPresentationInterpolation;
		}
	}
}