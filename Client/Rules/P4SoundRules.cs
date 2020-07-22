using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.Utility.Rules;
using Unity.Entities;

namespace PataNext.Client.Rules
{
	public class P4SoundRules : RuleBaseSystem<P4SoundRules.Data>
	{
		public RuleProperties<Data>.Property<bool> EnableDrumVoices;

		protected override void AddRuleProperties()
		{
			EnableDrumVoices = Rule.Add(null, d => d.EnableDrumVoices);
		}

		protected override void SetDefaultProperties()
		{
			EnableDrumVoices.Value = true;
		}

		public struct Data : IComponentData
		{
			public bool EnableDrumVoices;
		}
	}
}