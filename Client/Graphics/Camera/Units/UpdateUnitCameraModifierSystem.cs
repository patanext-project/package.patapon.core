using System;
using GameHost.ShareSimuWorldFeature.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Module.Simulation.Components;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.GamePlay.Units;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Components.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using StormiumTeam.GameBase.BaseSystems.Ext;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Utility.DOTS.xCamera;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PataNext.Client.Graphics.Camera.Units
{
	public class SetUnitArchetypeSystem : ComponentSystem
	{
		private EntityQuery m_EntityWithoutArchetype;

		protected override void OnCreate()
		{
			m_EntityWithoutArchetype = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(UnitDescription)},
				None = new ComponentType[] {typeof(IsSet)}
			});
		}

		protected override void OnUpdate()
		{
			EntityManager.AddComponent(m_EntityWithoutArchetype, typeof(CameraModifierData));
			EntityManager.AddComponent(m_EntityWithoutArchetype, typeof(CameraTargetAnchor));
			EntityManager.AddComponent(m_EntityWithoutArchetype, typeof(UpdateUnitCameraModifierSystem.CameraData));
			EntityManager.AddComponent(m_EntityWithoutArchetype, typeof(IsSet));
		}

		public struct IsSet : IComponentData
		{
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class UpdateUnitCameraModifierSystem : AbsGameBaseSystem
	{
		private InterFrame previousFrame;

		protected override void OnUpdate()
		{
			if (!HasSingleton<InterFrame>())
				return;

			var interFrame = GetSingleton<InterFrame>();

			var localPlayer = this.GetFirstSelfGamePlayer();
			EntityManager.TryGetComponentData(localPlayer, out PlayerInputComponent playerInput);

			var dt                  = Time.DeltaTime;
			var directionFromEntity = GetComponentDataFromEntity<UnitDirection>(true);

			var translationFromEntity    = GetComponentDataFromEntity<Translation>(true);
			var relativeTargetFromEntity = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);
			var ownerFromEntity          = GetComponentDataFromEntity<Owner>(true);

			var abilityControllerFromEntity = GetComponentDataFromEntity<OwnerActiveAbility>(true);
			var abilityActivationFromEntity = GetComponentDataFromEntity<AbilityActivation>(true);
			var abilityStateFromEntity      = GetComponentDataFromEntity<AbilityState>(true);
			Entities
				.WithAll<SetUnitArchetypeSystem.IsSet>()
				.ForEach((Entity entity, ref CameraModifierData cameraModifier, ref CameraTargetAnchor anchor, ref CameraData cameraData, in Translation translation, in UnitEnemySeekingState seekingState) =>
				{
					var direction = directionFromEntity.TryGet(entity, out _, UnitDirection.Right);

					var targetFocal  = seekingState.Enemy != default ? 7.8f : 5.7f;
					var targetOffset = seekingState.Enemy != default ? 0.66f : 0.33f;

					var isImmediate = false;
					var isSpecial   = false;

					if (abilityControllerFromEntity.TryGet(entity, out var controller)
					    && abilityActivationFromEntity.TryGet(controller.Incoming, out var activation) && activation.Type == EAbilityActivationType.HeroMode
					    && abilityStateFromEntity.TryGet(controller.Incoming, out var state) && (state.Phase & EAbilityPhase.ActiveOrChaining) == 0)
					{
						if (math.abs(playerInput.Panning) < 0.1f)
						{
							cameraData.HeroModeZoom            =  true;
							cameraData.HeroModeZoomProgression += dt;
						}
					}
					else
					{
						if (cameraData.HeroModeZoom)
							isImmediate = true;

						cameraData.HeroModeZoom            = false;
						cameraData.HeroModeZoomProgression = 0;
					}

					isSpecial |= cameraData.HeroModeZoom;

					if (!isSpecial)
					{
						if (isImmediate)
						{
							cameraModifier.FieldOfView = targetFocal;
						}
						else
						{
							cameraModifier.FieldOfView = Mathf.SmoothDamp(cameraModifier.FieldOfView, targetFocal, ref cameraData.FocalVelocity, 0.35f, 100, dt);
							cameraModifier.FieldOfView = math.max(cameraModifier.FieldOfView, 4);
						}
					}
					else
					{
						if (cameraData.HeroModeZoom)
						{
							cameraModifier.FieldOfView = math.lerp(7, 6f, math.min(cameraData.HeroModeZoomProgression * 2.5f, 1));
							if (cameraData.HeroModeZoomProgression >= 0.4f)
								cameraModifier.FieldOfView -= (cameraData.HeroModeZoomProgression - 0.4f) * 0.2f;

							cameraModifier.FieldOfView = math.max(cameraModifier.FieldOfView, 5.75f);
							cameraModifier.Position.x  = translation.Value.x;
						}
					}

					ownerFromEntity.TryGet(entity, out var owner);

					Relative<UnitTargetDescription> relativeTarget;
					if (!relativeTargetFromEntity.TryGet(entity, out relativeTarget))
						relativeTargetFromEntity.TryGet(owner.Target, out relativeTarget);

					if (!isSpecial)
					{
						var useTargetPosition = false;
						var targetPosition    = new float();
						if (relativeTarget.Target != default && seekingState.Enemy != default)
						{
							targetPosition    = translationFromEntity[relativeTarget.Target].Value.x;
							useTargetPosition = (targetPosition - translation.Value.x) * direction.Value > 0;
						}

						// in future, set y and z
						float3 positionResult = default;
						positionResult.x =  useTargetPosition ? targetPosition : translation.Value.x;
						positionResult.x += cameraModifier.FieldOfView * targetOffset * direction.Value;

						if (!isImmediate)
						{
							var panningModifier = playerInput.Panning * (cameraModifier.FieldOfView + 4f * direction.Value);

							var interFrameDelta = interFrame.Begin.Delta * (interFrame.End.Frame - interFrame.Begin.Frame + 1);
							if (interFrameDelta > 0)
							{
								// passing a value at 0 will set the Velocity to NaN
								cameraData.InterpolatedPosition = Mathf.SmoothDamp(cameraData.InterpolatedPosition, positionResult.x, ref cameraData.PositionVelocity, 0.5f, 100, interFrameDelta);
							}

							cameraData.InterpolatedPanning  = Mathf.SmoothDamp(cameraData.InterpolatedPanning, panningModifier, ref cameraData.PanningVelocity, 0.175f, 100, dt);

							cameraModifier.Position.x = cameraData.InterpolatedPosition + cameraData.InterpolatedPanning;
						}
						else
						{
							cameraModifier.Position.x       = positionResult.x;
							// make sure that the interpolation data is also immediate
							cameraData.InterpolatedPosition = positionResult.x;
						}
					}

					if (math.isnan(cameraModifier.Position.x) || math.abs(cameraModifier.Position.x) > 4000.0f)
					{
						cameraModifier.Position.x = 0;
					}
					
					anchor.Type  = AnchorType.Screen;
					anchor.Value = new float2(0, 0.7f);
				})
				.WithReadOnly(directionFromEntity)
				.WithReadOnly(relativeTargetFromEntity)
				.WithReadOnly(translationFromEntity)
				.WithReadOnly(ownerFromEntity)
				.WithReadOnly(abilityControllerFromEntity)
				.WithReadOnly(abilityStateFromEntity)
				.WithReadOnly(abilityActivationFromEntity)
				.Schedule();
		}

		public struct CameraData : IComponentData
		{
			public bool  HeroModeZoom;
			public float HeroModeZoomProgression;

			public float InterpolatedPosition;
			public float InterpolatedPanning;

			public float PositionVelocity;
			public float PanningVelocity;
			public float FocalVelocity;
		}
	}
}