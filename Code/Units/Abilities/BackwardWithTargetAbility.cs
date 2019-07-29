using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Patapon4TLB.Default
{
	public struct BackwardWithTargetAbility : IComponentData
	{
		public float AccelerationFactor;
		public float Time;
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class BackwardWithTargetAbilitySystem : JobGameBaseSystem
	{
		private struct JobProcess : IJobForEachWithEntity<Owner, RhythmAbilityState, BackwardWithTargetAbility>
		{
			public float DeltaTime;

			[ReadOnly] public ComponentDataFromEntity<GroundState>   GroundStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitPlayState> UnitPlayStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitDirection> UnitDirectionFromEntity;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<UnitTargetPosition> UnitTargetPositionFromEntity;

			public void Execute(Entity entity, int _, [ReadOnly] ref Owner owner, [ReadOnly] ref RhythmAbilityState state, ref BackwardWithTargetAbility BackwardAbility)
			{
				if (!state.IsActive)
				{
					BackwardAbility.Time = 0;
					return;
				}

				var unitPlayState  = UnitPlayStateFromEntity[owner.Target];
				var unitDirection  = UnitDirectionFromEntity[owner.Target];
				var targetPosition = UnitTargetPositionFromEntity[owner.Target];
				var groundState    = GroundStateFromEntity[owner.Target];

				if (!groundState.Value)
					return;

				// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
				var acceleration = BackwardAbility.AccelerationFactor;
				acceleration = math.min(acceleration * DeltaTime, 1);

				var walkSpeed = unitPlayState.MovementSpeed;
				BackwardAbility.Time += DeltaTime;

				walkSpeed *= -0.5f;

				targetPosition.Value.x += walkSpeed * unitDirection.Value * (BackwardAbility.Time > 0.5f ? 1 : math.lerp(4, 1, BackwardAbility.Time + 0.5f)) * acceleration;

				UnitTargetPositionFromEntity[owner.Target] = targetPosition;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (!IsServer)
				return inputDeps;

			return new JobProcess
			{
				DeltaTime                    = GetSingleton<GameTimeComponent>().DeltaTime,
				UnitPlayStateFromEntity      = GetComponentDataFromEntity<UnitPlayState>(true),
				UnitDirectionFromEntity      = GetComponentDataFromEntity<UnitDirection>(true),
				GroundStateFromEntity        = GetComponentDataFromEntity<GroundState>(true),
				UnitTargetPositionFromEntity = GetComponentDataFromEntity<UnitTargetPosition>(),
			}.Schedule(this, inputDeps);
		}
	}

	public class BackwardWithTargetAbilityProvider : BaseProviderBatch<BackwardWithTargetAbilityProvider.Create>
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
				typeof(BackwardWithTargetAbility),
				typeof(Owner),
				typeof(DestroyChainReaction)
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, new RhythmAbilityState {Command                   = data.Command});
			EntityManager.SetComponentData(entity, new BackwardWithTargetAbility {AccelerationFactor = data.AccelerationFactor});
			EntityManager.SetComponentData(entity, new Owner {Target                                 = data.Owner});
			EntityManager.SetComponentData(entity, new DestroyChainReaction(data.Owner));
		}
	}
}