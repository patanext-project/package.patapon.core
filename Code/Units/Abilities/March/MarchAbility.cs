using System;
using System.Collections.Generic;
using package.patapon.core;
using package.patapon.core.Animation;
using package.StormiumTeam.GameBase;
using Patapon4TLB.Core;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Patapon4TLB.Default
{
	public struct MarchAbility : IComponentData
	{
		public float AccelerationFactor;
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class MarchAbilitySystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct JobProcess : IJobForEachWithEntity<Owner, RhythmAbilityState, MarchAbility>
		{
			public float DeltaTime;

			[ReadOnly] public ComponentDataFromEntity<Translation>        TranslationFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitPlayState>      UnitPlayStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GroundState>        GroundStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitTargetPosition> UnitTargetPositionFromEntity;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitControllerState> UnitControllerStateFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Velocity>            VelocityFromEntity;

			public void Execute(Entity entity, int _, [ReadOnly] ref Owner owner, [ReadOnly] ref RhythmAbilityState state, [ReadOnly] ref MarchAbility marchAbility)
			{
				if (!state.IsActive)
					return;

				var targetPosition = UnitTargetPositionFromEntity[owner.Target];
				var groundState    = GroundStateFromEntity[owner.Target];

				if (!groundState.Value)
					return;

				var unitPlayState = UnitPlayStateFromEntity[owner.Target];
				var velocity      = VelocityFromEntity[owner.Target];

				// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
				var acceleration = math.clamp(math.rcp(unitPlayState.Weight), 0, 1) * marchAbility.AccelerationFactor * 50;
				acceleration = math.min(acceleration * DeltaTime, 1);

				var walkSpeed = unitPlayState.MovementSpeed;
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
				DeltaTime                     = World.GetExistingSystem<ServerSimulationSystemGroup>().UpdateDeltaTime,
				UnitPlayStateFromEntity       = GetComponentDataFromEntity<UnitPlayState>(true),
				TranslationFromEntity         = GetComponentDataFromEntity<Translation>(true),
				GroundStateFromEntity         = GetComponentDataFromEntity<GroundState>(true),
				UnitTargetPositionFromEntity  = GetComponentDataFromEntity<UnitTargetPosition>(true),
				UnitControllerStateFromEntity = GetComponentDataFromEntity<UnitControllerState>(),
				VelocityFromEntity            = GetComponentDataFromEntity<Velocity>()
			}.Schedule(this, inputDeps);
		}
	}

	public class MarchAbilityProvider : BaseProviderBatch<MarchAbilityProvider.Create>
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
				typeof(MarchAbility),
				typeof(Owner),
				typeof(DestroyChainReaction)
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, new RhythmAbilityState {Command      = data.Command});
			EntityManager.SetComponentData(entity, new MarchAbility {AccelerationFactor = data.AccelerationFactor});
			EntityManager.SetComponentData(entity, new Owner {Target                    = data.Owner});
			EntityManager.SetComponentData(entity, new DestroyChainReaction(data.Owner));
		}
	}
}