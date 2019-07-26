using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace Patapon4TLB.Default.Attack
{
	public struct BasicTaterazayAttackAbility : IComponentData
	{
		public int LastAttackTick;

		[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
		public class Process : JobGameBaseSystem
		{
			private struct Job : IJobForEach<RhythmAbilityState, BasicTaterazayAttackAbility>
			{
				public int CurrentTick;
			
				public void Execute(ref RhythmAbilityState state, ref BasicTaterazayAttackAbility ability)
				{
					if (!state.IsActive)
						return;
					
					if (ability.LastAttackTick + 500 < CurrentTick)
					{
						ability.LastAttackTick = CurrentTick;
						Debug.Log("slash!");
					}
				}
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				return new Job
				{
					CurrentTick = GetSingleton<GameTimeComponent>().Tick,
				}.Schedule(this, inputDeps);
			}
		}

		public struct Create
		{
			public Entity Owner;
			public Entity Command;
		}

		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(ActionDescription),
					typeof(RhythmAbilityState),
					typeof(BasicTaterazayAttackAbility),
					typeof(Owner),
					typeof(DestroyChainReaction)
				};
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.ReplaceOwnerData(entity, data.Owner);
				EntityManager.SetComponentData(entity, new RhythmAbilityState {Command                 = data.Command});
				EntityManager.SetComponentData(entity, new BasicTaterazayAttackAbility {LastAttackTick = -1000});
				EntityManager.SetComponentData(entity, new Owner {Target                               = data.Owner});
				EntityManager.SetComponentData(entity, new DestroyChainReaction(data.Owner));
			}
		}
	}
}