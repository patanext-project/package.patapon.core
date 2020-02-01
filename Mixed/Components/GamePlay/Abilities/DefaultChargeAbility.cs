using System;
using P4TLB.MasterServer;
using Patapon.Mixed.RhythmEngine;
using Revolution;
using Unity.Entities;

namespace Patapon.Mixed.GamePlay.Abilities
{
	public struct DefaultChargeAbility : IComponentData
	{
		public int foo;
		
		public class Provider : BaseRhythmAbilityProvider<DefaultChargeAbility>
		{
			public override string MasterServerId  => nameof(P4OfficialAbilities.BasicCharge);
			public override Type   ChainingCommand => typeof(ChargeCommand);
		}

		public class EmptySynchronize : ComponentSnapshotSystemTag<DefaultChargeAbility>
		{
		}
	}
}