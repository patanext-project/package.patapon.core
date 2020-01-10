using DefaultNamespace;
using package.patapon.core;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.Units;
using Revolution;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace SnapshotArchetypes
{
	[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
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
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UpdateUnitCameraModifierSystem : JobGameBaseSystem
	{
		private LazySystem<GrabInputSystem> m_GrabInputSystem;

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var userCommand         = this.L(ref m_GrabInputSystem).LocalCommand;
			var dt                  = Time.DeltaTime;
			var directionFromEntity = GetComponentDataFromEntity<UnitDirection>(true);

			var translationFromEntity    = GetComponentDataFromEntity<Translation>(true);
			var relativeTargetFromEntity = GetComponentDataFromEntity<Relative<UnitTargetDescription>>(true);
			var ownerFromEntity          = GetComponentDataFromEntity<Owner>(true);

			inputDeps = Entities
			            .WithAll<SetUnitArchetypeSystem.IsSet>()
			            .ForEach((Entity entity, ref CameraModifierData cameraModifier, ref CameraTargetAnchor anchor, ref CameraData cameraData, in Translation translation, in UnitEnemySeekingState seekingState) =>
			            {
				            var direction = directionFromEntity.TryGet(entity, out _, UnitDirection.Right);

				            var targetFocal = seekingState.Enemy != default ? 7f : 5f;
				            cameraModifier.FieldOfView = Mathf.SmoothDamp(cameraModifier.FieldOfView, targetFocal, ref cameraData.FocalVelocity, 0.35f, 100, dt); // add enemies seeking...
				            cameraModifier.FieldOfView = math.clamp(cameraModifier.FieldOfView, 5, 7);

				            ownerFromEntity.TryGet(entity, out var owner);

				            Relative<UnitTargetDescription> relativeTarget;
				            if (!relativeTargetFromEntity.TryGet(entity, out relativeTarget))
					            relativeTargetFromEntity.TryGet(owner.Target, out relativeTarget);

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
				            positionResult.x += userCommand.Panning * (cameraModifier.FieldOfView + 2.5f * direction.Value);
				            positionResult.x += cameraModifier.FieldOfView * 0.375f * direction.Value;

				            cameraModifier.Position.x = Mathf.SmoothDamp(cameraModifier.Position.x, positionResult.x, ref cameraData.PositionVelocity, 0.4f, 100, dt);
				            if (math.isnan(cameraModifier.Position.x) || math.abs(cameraModifier.Position.x) > 4000.0f) cameraModifier.Position.x = 0;

				            Debug.DrawRay(cameraModifier.Position, Vector3.up * 4, Color.blue);

				            anchor.Type  = AnchorType.Screen;
				            anchor.Value = new float2(0, 0.7f);
			            })
			            .WithReadOnly(directionFromEntity)
			            .WithReadOnly(relativeTargetFromEntity)
			            .WithReadOnly(translationFromEntity)
			            .WithReadOnly(ownerFromEntity)
			            .Schedule(inputDeps);

			return inputDeps;
		}

		public struct CameraData : IComponentData
		{
			public float PositionVelocity;
			public float FocalVelocity;
		}
	}
}