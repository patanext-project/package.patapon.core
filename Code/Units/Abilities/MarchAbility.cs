using package.patapon.core;
using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

namespace Patapon4TLB.Default
{
	public struct MarchAbility : IComponentData
	{
		public float AccelerationFactor;
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class MarchAbilitySystem : JobGameBaseSystem
	{
		private struct JobProcess : IJobForEachWithEntity<Owner, RhythmAbilityState, MarchAbility>
		{
			public float DeltaTime;

			[ReadOnly]
			public ComponentDataFromEntity<UnitRhythmState> UnitStateFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<GroundState> GroundStateFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<UnitBaseSettings> UnitSettingsFromEntity;

			[ReadOnly]
			public ComponentDataFromEntity<UnitDirection> UnitDirectionFromEntity;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<PhysicsVelocity> VelocityFromEntity;

			public void Execute(Entity entity, int _, [ReadOnly] ref Owner owner, [ReadOnly] ref RhythmAbilityState state, [ReadOnly] ref MarchAbility marchAbility)
			{
				if (!state.IsActive)
					return;

				var unitSettings  = UnitSettingsFromEntity[owner.Target];
				var unitDirection = UnitDirectionFromEntity[owner.Target];
				var groundState   = GroundStateFromEntity[owner.Target];

				if (!groundState.Value)
					return;

				var combo    = UnitStateFromEntity[owner.Target].Combo;
				var velocity = VelocityFromEntity[owner.Target];

				// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
				var acceleration = math.clamp(math.rcp(unitSettings.Weight), 0, 1) * marchAbility.AccelerationFactor;
				acceleration = math.min(acceleration * DeltaTime, 1);

				var walkSpeed = unitSettings.BaseWalkSpeed;
				if (combo.IsFever)
				{
					walkSpeed = unitSettings.FeverWalkSpeed;
				}

				velocity.Linear.x = math.lerp(velocity.Linear.x, walkSpeed * unitDirection.Value, acceleration);

				VelocityFromEntity[owner.Target] = velocity;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (!IsServer)
				return inputDeps;

			return new JobProcess
			{
				DeltaTime               = GetSingleton<GameTimeComponent>().DeltaTime,
				UnitStateFromEntity     = GetComponentDataFromEntity<UnitRhythmState>(true),
				UnitSettingsFromEntity  = GetComponentDataFromEntity<UnitBaseSettings>(true),
				UnitDirectionFromEntity = GetComponentDataFromEntity<UnitDirection>(true),
				GroundStateFromEntity   = GetComponentDataFromEntity<GroundState>(),
				VelocityFromEntity      = GetComponentDataFromEntity<PhysicsVelocity>()
			}.Schedule(this, inputDeps);
		}
	}
}