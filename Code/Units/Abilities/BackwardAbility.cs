using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Revolution.NetCode;
using Unity.Transforms;

namespace Patapon4TLB.Default
{
	public struct BackwardAbility : IComponentData
	{
		public float AccelerationFactor;
		public float Delta;
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class BackwardAbilitySystem : JobGameBaseSystem
	{
		private struct JobProcess : IJobForEachWithEntity<Owner, RhythmAbilityState, BackwardAbility, Relative<UnitTargetDescription>>
		{
			public float DeltaTime;

			[ReadOnly] public ComponentDataFromEntity<UnitTargetControlTag> UnitTargetControlFromEntity;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Translation>   TranslationFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitPlayState> UnitPlayStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GroundState>   GroundStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitDirection> UnitDirectionFromEntity;

			[ReadOnly] public ComponentDataFromEntity<UnitTargetOffset> TargetOffsetFromEntity;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitControllerState> UnitControllerStateFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Velocity>            VelocityFromEntity;

			public void Execute(Entity                                         entity, int                 _, [ReadOnly] ref Owner owner,
			                    [ReadOnly] ref RhythmAbilityState              state,  ref BackwardAbility backwardAbility,
			                    [ReadOnly] ref Relative<UnitTargetDescription> relativeTarget)
			{
				if (!state.IsActive)
				{
					backwardAbility.Delta = 0.0f;
					return;
				}

				var targetOffset  = TargetOffsetFromEntity[owner.Target];
				var groundState   = GroundStateFromEntity[owner.Target];
				var unitPlayState = UnitPlayStateFromEntity[owner.Target];

				if (!groundState.Value)
					return;

				if (state.Combo.IsFever && state.Combo.Score >= 50)
				{
					unitPlayState.MovementSpeed *= 1.2f;
				}

				backwardAbility.Delta += DeltaTime;

				var   targetPosition = TranslationFromEntity[relativeTarget.Target].Value;
				float acceleration, walkSpeed;
				int   direction;

				if (UnitTargetControlFromEntity.Exists(owner.Target))
				{
					direction = UnitDirectionFromEntity[owner.Target].Value;

					// a different acceleration (not using the unit weight)
					acceleration = backwardAbility.AccelerationFactor;
					acceleration = math.min(acceleration * DeltaTime, 1);

					backwardAbility.Delta += DeltaTime;

					walkSpeed      =  unitPlayState.MovementSpeed * -0.5f;
					targetPosition += walkSpeed * direction * (backwardAbility.Delta > 0.5f ? 1 : math.lerp(4, 1, backwardAbility.Delta + 0.5f)) * acceleration;

					TranslationFromEntity[relativeTarget.Target] = new Translation {Value = targetPosition};
				}

				var velocity = VelocityFromEntity[owner.Target];

				// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
				acceleration = math.clamp(math.rcp(unitPlayState.Weight), 0, 1) * backwardAbility.AccelerationFactor * 50;
				acceleration = math.min(acceleration * DeltaTime, 1);

				walkSpeed = unitPlayState.MovementSpeed * 0.5f;
				// if we're near, let's slow down
				var dist = math.distance(targetPosition.x, TranslationFromEntity[owner.Target].Value.x);
				if (dist < 0.5f)
				{
					walkSpeed *= math.clamp(dist * 2f, 0.5f, 1.0f);
				}

				direction = System.Math.Sign(targetPosition.x + targetOffset.Value - TranslationFromEntity[owner.Target].Value.x);

				velocity.Value.x                 = math.lerp(velocity.Value.x, walkSpeed * direction, acceleration);
				VelocityFromEntity[owner.Target] = velocity;

				var controllerState = UnitControllerStateFromEntity[owner.Target];
				controllerState.ControlOverVelocity.x       = true;
				UnitControllerStateFromEntity[owner.Target] = controllerState;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (!IsServer)
				return inputDeps;

			return new JobProcess
			{
				DeltaTime                     = World.GetExistingSystem<ServerSimulationSystemGroup>().UpdateDeltaTime,
				UnitTargetControlFromEntity   = GetComponentDataFromEntity<UnitTargetControlTag>(true),
				UnitDirectionFromEntity       = GetComponentDataFromEntity<UnitDirection>(true),
				UnitPlayStateFromEntity       = GetComponentDataFromEntity<UnitPlayState>(true),
				TranslationFromEntity         = GetComponentDataFromEntity<Translation>(),
				GroundStateFromEntity         = GetComponentDataFromEntity<GroundState>(true),
				TargetOffsetFromEntity        = GetComponentDataFromEntity<UnitTargetOffset>(true),
				UnitControllerStateFromEntity = GetComponentDataFromEntity<UnitControllerState>(),
				VelocityFromEntity            = GetComponentDataFromEntity<Velocity>()
			}.Schedule(this, inputDeps);
		}
	}

	public class BackwardAbilityProvider : BaseProviderBatch<BackwardAbilityProvider.Create>
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
				typeof(BackwardAbility),
				typeof(Owner),
				typeof(DestroyChainReaction),
				
				typeof(PlayEntityTag),
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, new RhythmAbilityState {Command         = data.Command});
			EntityManager.SetComponentData(entity, new BackwardAbility {AccelerationFactor = data.AccelerationFactor});
			EntityManager.SetComponentData(entity, new Owner {Target                       = data.Owner});
			EntityManager.SetComponentData(entity, new DestroyChainReaction(data.Owner));
		}
	}
}