using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.Units;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Systems.GamePlay
{
	public struct AbilityControlVelocity : IComponentData
	{
		public bool IsActive;

		public bool3  Control;

		public bool TargetFromCursor;
		public float3 TargetPosition;
		public float  Acceleration;

		public bool HasCustomMovementSpeed;
		public float CustomMovementSpeed;
	}

	[UpdateInGroup(typeof(UnitPhysicSystemGroup))]
	[UpdateBefore(typeof(UnitInitStateSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class AbilityControlOwnerTargetSystem : AbsGameBaseSystem
	{
		struct Payload
		{
			[ReadOnly]
			public ComponentDataFromEntity<Translation> Translation;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<Velocity> Velocity;

			[NativeDisableParallelForRestriction]
			public ComponentDataFromEntity<UnitControllerState> ControllerState;

			[ReadOnly]
			public ComponentDataFromEntity<UnitPlayState> PlayState;

			[ReadOnly]
			public ComponentDataFromEntity<Relative<UnitTargetDescription>> CursorRelative;
		}

		protected override void OnUpdate()
		{
			var payload = new Payload
			{
				Translation     = GetComponentDataFromEntity<Translation>(true),
				PlayState       = GetComponentDataFromEntity<UnitPlayState>(true),
				Velocity        = GetComponentDataFromEntity<Velocity>(),
				ControllerState = GetComponentDataFromEntity<UnitControllerState>(),
				CursorRelative  = GetComponentDataFromEntity<Relative<UnitTargetDescription>>()
			};
			var tick = ServerTick;

			// allow parallel operations since only one ability can be active on an owner...
			Entities.ForEach((ref AbilityControlVelocity target, in Owner owner) =>
			{
				if (!target.IsActive)
					return;
				target.IsActive = false;

				var position  = payload.Translation[owner.Target].Value;
				var playState = payload.PlayState[owner.Target];

				var velocityUpdater   = payload.Velocity.GetUpdater(owner.Target).Out(out var velocity);
				var controllerUpdater = payload.ControllerState.GetUpdater(owner.Target).Out(out var controller);

				if (target.HasCustomMovementSpeed)
					playState.MovementAttackSpeed = target.CustomMovementSpeed;

				var targetPosition = target.TargetPosition;
				if (target.TargetFromCursor && payload.CursorRelative.TryGet(owner.Target, out var cursorRelative)
				                            && payload.Translation.TryGet(cursorRelative.Target, out var cursorTranslation))
				{
					targetPosition += cursorTranslation.Value.x;
				}

				velocity.Value.x = AbilityUtility.GetTargetVelocityX(new AbilityUtility.GetTargetVelocityParameters
				{
					TargetPosition   = targetPosition,
					PreviousPosition = position,
					PreviousVelocity = velocity.Value,
					PlayState        = playState,
					Acceleration     = target.Acceleration,
					Tick             = tick
				}, 0, 0.5f);
				controller.ControlOverVelocity.x = true;

				velocityUpdater.Update(velocity);
				controllerUpdater.CompareAndUpdate(controller);
			}).ScheduleParallel();
		}
	}
}