using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Unity.Entities;

namespace Patapon.Mixed.GamePlay.Abilities
{
	// Active only when you're eliminated
	public struct DefaultRebornAbility : IComponentData
	{
		public bool WasFever;
		public int  LastPressureBeat;

		public class Provider : BaseRhythmAbilityProvider<DefaultRebornAbility>
		{
			public override bool UseOldRhythmAbilityState => true;

			public override string MasterServerId  => nameof(P4OfficialAbilities.NoneOrCustom);
			public override Type   ChainingCommand => typeof(SummonCommand);
		}
	}
}