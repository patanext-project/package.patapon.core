using StormiumTeam.GameBase.BaseSystems;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace DefaultNamespace
{
	public class P4ColorRules : RuleBaseSystem<P4ColorRules.Data>
	{
		public RuleProperties<Data>.Property<Color> UnitNoTeamColor;
		public RuleProperties<Data>.Property<Color> UnitOwnedColor;

		protected override void AddRuleProperties()
		{
			UnitNoTeamColor = Rule.Add(d => d.UnitNoTeamColor);
			UnitOwnedColor = Rule.Add(d => d.UnitOwnedColor);
		}

		protected override void SetDefaultProperties()
		{
			UnitNoTeamColor.Value = new Color(0.96f, 1f, 0.86f);
			UnitOwnedColor.Value = new Color(1f, 0.78f, 0.13f);
		}

		public struct Data : IComponentData
		{
			public Color UnitNoTeamColor;
			public Color UnitOwnedColor;
		}
	}
}