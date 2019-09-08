using package.patapon.core;
using Patapon4TLB.Default;
using Patapon4TLB.Default.Attack;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Revolution.NetCode;

namespace Patapon4TLBCore
{
	public struct BotControlledUnit : IComponentData
	{

	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public class BotSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.WithAll<BotControlledUnit>().ForEach((Entity unit, ref LivableHealth health, DynamicBuffer<ActionContainer> abilities) =>
			{
				for (var ab = 0; ab != abilities.Length; ab++)
				{
					var ability = abilities[ab].Target;
					if (ability == default)
						return;

					if (EntityManager.HasComponent<BasicTaterazayAttackAbility>(ability))
					{
						var rhythmState = EntityManager.GetComponentData<RhythmAbilityState>(ability);
						if (!health.IsDead)
						{
							rhythmState.CalculateWithValidCommand(new GameCommandState
							{
								StartTime    = 1000,
								EndTime      = 5000,
								ChainEndTime = 5000
							}, new GameComboState
							{
								Chain        = 42,
								ChainToFever = 42,
								IsFever      = false,
								Score        = 10
							}, new RhythmEngineProcess
							{
								Milliseconds = 2500,
								StartTime    = 1
							});
						}
						else
						{
							rhythmState.IsActive = false;
							rhythmState.IsStillChaining = false;
							rhythmState.WillBeActive = false;
						}

						EntityManager.SetComponentData(ability, rhythmState);
					}
				}
			});
		}
	}
}