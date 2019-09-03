using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;

namespace Patapon4TLB.Default
{
	public struct JumpAbility : IComponentData
	{
		public int LastActiveId;
		
		public bool   IsJumping;
		public float  ActiveTime;
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class JumpAbilitySystem : JobGameBaseSystem
	{
		private struct Job : IJobForEach<Owner, RhythmAbilityState, JumpAbility>
		{
			[ReadOnly] public float DeltaTime;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitControllerState> UnitControllerStateFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Velocity>            VelocityFromEntity;

			public void Execute(ref Owner owner, ref RhythmAbilityState state, ref JumpAbility ability)
			{
				if (state.ActiveId != ability.LastActiveId)
				{
					ability.IsJumping    = false;
					ability.ActiveTime   = 0;
					ability.LastActiveId = state.ActiveId;
				}

				if (!state.IsActive && !state.IsStillChaining)
				{
					if (ability.IsJumping)
					{
						var temp = VelocityFromEntity[owner.Target];
						temp.Value.y                     = math.max(0, temp.Value.y - 20 * (ability.ActiveTime * 2));
						VelocityFromEntity[owner.Target] = temp;
					}

					ability.ActiveTime = 0;
					ability.IsJumping  = false;
					return;
				}

				var wasJumping = ability.IsJumping;
				ability.IsJumping = ability.ActiveTime <= 0.5f;

				var velocity = VelocityFromEntity[owner.Target];
				if (!wasJumping && ability.IsJumping)
				{
					velocity.Value.y = math.max(velocity.Value.y + 20, 20);
				}

				if (ability.ActiveTime < 3.25f)
					velocity.Value.x = math.lerp(velocity.Value.x, 0, DeltaTime * (ability.ActiveTime + 1));

				if (!ability.IsJumping && wasJumping)
					velocity.Value.y = 0;

				ability.ActiveTime += DeltaTime;

				VelocityFromEntity[owner.Target] = velocity;

				var controllerState = UnitControllerStateFromEntity[owner.Target];
				controllerState.ControlOverVelocity.x       = ability.ActiveTime < 3.25f;
				controllerState.ControlOverVelocity.y       = ability.ActiveTime < 2.5f;
				UnitControllerStateFromEntity[owner.Target] = controllerState;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new Job
			{
				DeltaTime                     = World.GetExistingSystem<ServerSimulationSystemGroup>().UpdateDeltaTime,
				UnitControllerStateFromEntity = GetComponentDataFromEntity<UnitControllerState>(),
				VelocityFromEntity            = GetComponentDataFromEntity<Velocity>()
			}.Schedule(this, inputDeps);
		}
	}

	public class JumpAbilityProvider : BaseProviderBatch<JumpAbilityProvider.Create>
	{
		public struct Create
		{
			public Entity Owner;
			public Entity Command;
			public float  AccelerationFactor;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(ActionDescription),
				typeof(RhythmAbilityState),
				typeof(JumpAbility),
				typeof(Owner),
				typeof(DestroyChainReaction),
				
				typeof(PlayEntityTag),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, new RhythmAbilityState {Command        = data.Command});
			EntityManager.SetComponentData(entity, new JumpAbility {});
			EntityManager.SetComponentData(entity, new Owner {Target                      = data.Owner});
			EntityManager.SetComponentData(entity, new DestroyChainReaction(data.Owner));
		}
	}
}