using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Patapon.Mixed.GamePlay
{
	public struct BasicUnitAbilityImplementation
	{
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitStatistics> UnitSettingsFromEntity;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitPlayState>  UnitPlayStateFromEntity;

		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Translation>         TranslationFromEntity;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<LocalToWorld>        LocalToWorldFromEntity;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitControllerState> ControllerFromEntity;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Velocity>            VelocityFromEntity;
		[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitDirection>       UnitDirectionFromEntity;

		public BasicUnitAbilityImplementation(ComponentSystemBase system)
		{
			UnitSettingsFromEntity  = system.GetComponentDataFromEntity<UnitStatistics>();
			UnitPlayStateFromEntity = system.GetComponentDataFromEntity<UnitPlayState>();

			TranslationFromEntity   = system.GetComponentDataFromEntity<Translation>();
			LocalToWorldFromEntity  = system.GetComponentDataFromEntity<LocalToWorld>();
			ControllerFromEntity    = system.GetComponentDataFromEntity<UnitControllerState>();
			VelocityFromEntity      = system.GetComponentDataFromEntity<Velocity>();
			UnitDirectionFromEntity = system.GetComponentDataFromEntity<UnitDirection>();
		}
	}
}