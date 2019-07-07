using package.patapon.core;
using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.Default
{
	public struct BackwardAbility : IComponentData
	{
		public float AccelerationFactor;
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class BackwardAbilitySystem : JobGameBaseSystem
	{
		private struct JobProcess : IJobForEachWithEntity<Owner, RhythmAbilityState, BackwardAbility>
		{
			public float DeltaTime;

			[ReadOnly] public ComponentDataFromEntity<Translation>        TranslationFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitRhythmState>    UnitStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GroundState>        GroundStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitBaseSettings>   UnitSettingsFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitTargetPosition> UnitTargetPositionFromEntity;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitControllerState> UnitControllerStateFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Velocity>            VelocityFromEntity;

			public void Execute(Entity entity, int _, [ReadOnly] ref Owner owner, [ReadOnly] ref RhythmAbilityState state, [ReadOnly] ref BackwardAbility BackwardAbility)
			{
				if (!state.IsActive)
					return;

				var unitSettings   = UnitSettingsFromEntity[owner.Target];
				var targetPosition = UnitTargetPositionFromEntity[owner.Target];
				var groundState    = GroundStateFromEntity[owner.Target];

				if (!groundState.Value)
					return;

				var combo    = UnitStateFromEntity[owner.Target].Combo;
				var velocity = VelocityFromEntity[owner.Target];

				// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
				var acceleration = math.clamp(math.rcp(unitSettings.Weight), 0, 1) * BackwardAbility.AccelerationFactor * 50;
				acceleration = math.min(acceleration * DeltaTime, 1);

				var walkSpeed = unitSettings.BaseWalkSpeed;
				if (combo.IsFever)
				{
					walkSpeed = unitSettings.FeverWalkSpeed;
				}

				// if we're near, let's slow down
				var dist = math.distance(targetPosition.Value.x, TranslationFromEntity[owner.Target].Value.x);
				if (dist < 2f)
				{
					walkSpeed *= dist * 0.5f;
				}

				var direction = System.Math.Sign(targetPosition.Value.x - TranslationFromEntity[owner.Target].Value.x);

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
				DeltaTime                     = GetSingleton<GameTimeComponent>().DeltaTime,
				UnitStateFromEntity           = GetComponentDataFromEntity<UnitRhythmState>(true),
				UnitSettingsFromEntity        = GetComponentDataFromEntity<UnitBaseSettings>(true),
				TranslationFromEntity         = GetComponentDataFromEntity<Translation>(true),
				GroundStateFromEntity         = GetComponentDataFromEntity<GroundState>(true),
				UnitTargetPositionFromEntity  = GetComponentDataFromEntity<UnitTargetPosition>(true),
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
				typeof(DestroyChainReaction)
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