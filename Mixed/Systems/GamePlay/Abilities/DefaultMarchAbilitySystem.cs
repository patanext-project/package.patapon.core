using System;
using Systems.GamePlay.CYari;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Systems.GamePlay
{
	public class DefaultMarchAbilitySystem : BaseAbilitySystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Entity entity, ref DefaultMarchAbility marchAbility, ref DefaultSubsetMarch subSet, in AbilityState controller) =>
			        {
				        if ((controller.Phase & EAbilityPhase.Active) == 0)
				        {
					        subSet.IsActive = false;
					        return;
				        }

				        subSet.IsActive = true;
			        })
			        .Run();
		}
	}
}