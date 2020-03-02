using System;
using Systems.GamePlay.CYari;
using P4TLB.MasterServer;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.RhythmEngine;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Systems.GamePlay.CPingrek
{
	public struct PingrekIceAttack : IComponentData
	{
		public float AttackDelay;
		public UTick AttackTick;

		public class Provider : BaseRhythmAbilityProvider<PingrekIceAttack>
		{
			public override string MasterServerId => nameof(P4OfficialAbilities.MahosuIceAttack);
			public override Type ChainingCommand => typeof(AttackCommand);

			public override void SetEntityData(Entity entity, CreateAbility data)
			{
				base.SetEntityData(entity, data);
				// for later lul
			}
		}

		public class System : BaseAbilitySystem
		{
			protected override void OnUpdate()
			{
				var impl = new BasicUnitAbilityImplementation(this);
				
				Entities.ForEach((ref PingrekIceAttack ability, ref AbilityControlVelocity control, in AbilityState state, in AbilityEngineSet engineSet, in Owner owner) =>
				{
					if (!impl.CanExecuteAbility(owner.Target))
						return;
					
					
				}).Run();
			}
		}
	}
}