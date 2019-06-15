using Patapon4TLB.Default.Test;
using Runtime.BaseSystems;
using Unity.Entities;
using Unity.Jobs;

namespace P4.Core
{
	[AlwaysUpdateSystem]
	public class P4GameRuleSystem : RuleBaseSystem
	{
		public struct RuleData : IComponentData
		{
			public bool VoiceOverlay;
		}

		public override string Name => "Beat Sounds";

		public RuleProperties<RuleData>                Properties;
		public RuleProperties<RuleData>.Property<bool> VoiceOverlayProperty;

		protected override void OnCreate()
		{
			base.OnCreate();

			Properties           = AddRule<RuleData>(out var data);
			VoiceOverlayProperty = Properties.Add("Voice over drums", ref data, ref data.VoiceOverlay);

			VoiceOverlayProperty.Value = true;
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var activeClientWorld = GetActiveClientWorld();
			if (activeClientWorld == null)
				return inputDeps;

			var playBeatSoundSystem = activeClientWorld.GetExistingSystem<PlayBeatSound>();
			if (playBeatSoundSystem != null)
			{
				playBeatSoundSystem.VoiceOverlay = VoiceOverlayProperty.Value;
			}

			return inputDeps;
		}
	}
}