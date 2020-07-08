using package.stormiumteam.shared.ecs;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace Patapon.Mixed.GamePlay
{
	public struct BasicUnitAbilityImplementation
	{
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitStatistics> UnitSettings;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitPlayState>  UnitPlayState;

		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Translation>         Translation;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<LocalToWorld>        LocalToWorld;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitControllerState> Controller;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<SVelocity>            Velocity;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitDirection>       UnitDirection;

		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<GroundState> GroundState;

		[NativeDisableContainerSafetyRestriction]
		public ComponentDataFromEntity<LivableHealth> Health;

		public BasicUnitAbilityImplementation(ComponentSystemBase system)
		{
			UnitSettings  = system.GetComponentDataFromEntity<UnitStatistics>();
			UnitPlayState = system.GetComponentDataFromEntity<UnitPlayState>();

			Translation   = system.GetComponentDataFromEntity<Translation>();
			LocalToWorld  = system.GetComponentDataFromEntity<LocalToWorld>();
			Controller    = system.GetComponentDataFromEntity<UnitControllerState>();
			Velocity      = system.GetComponentDataFromEntity<SVelocity>();
			UnitDirection = system.GetComponentDataFromEntity<UnitDirection>();

			GroundState = system.GetComponentDataFromEntity<GroundState>();

			Health = system.GetComponentDataFromEntity<LivableHealth>(true);
		}

		public bool IsGrounded(Entity entity)
		{
			if (!GroundState.TryGet(entity, out var state))
				return true;
			return state.Value;
		}

		public bool CanExecuteAbility(Entity entity)
		{
			if (!Health.TryGet(entity, out var health))
				return true;
			return !health.IsDead;
		}
	}
}