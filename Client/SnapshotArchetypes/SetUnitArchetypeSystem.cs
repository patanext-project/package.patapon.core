using DefaultNamespace;
using package.patapon.core;
using package.stormiumteam.shared.ecs;
using Patapon.Mixed.GamePlay.RhythmEngine;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Units;
using Patapon4TLB.Default.Player;
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
		public struct IsSet : IComponentData
		{
		}

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
			EntityManager.AddComponent(m_EntityWithoutArchetype, typeof(IsSet));
		}
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class UpdateUnitCameraModifierSystem : JobGameBaseSystem
	{
		private LazySystem<GrabInputSystem> m_GrabInputSystem;
		
		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var userCommand = this.L(ref m_GrabInputSystem).LocalCommand;
			var dt = Time.DeltaTime;
			var directionFromEntity = GetComponentDataFromEntity<UnitDirection>(true);

			inputDeps = Entities
			            .WithAll<SetUnitArchetypeSystem.IsSet>()
			            .ForEach((Entity entity, ref CameraModifierData cameraModifier, ref CameraTargetAnchor anchor, ref Velocity velocity, in Translation translation) =>
			            {
				            var direction = directionFromEntity.TryGet(entity, out _, UnitDirection.Right);

				            cameraModifier.FieldOfView = 7; // add enemies seeking...

				            // in future, set y and z
				            float3 positionResult = default;
				            positionResult.x =  translation.Value.x;
				            positionResult.x += userCommand.Panning * (cameraModifier.FieldOfView + 2.5f * direction.Value);
				            positionResult.x += cameraModifier.FieldOfView * 0.375f * direction.Value;
				            
				            cameraModifier.Position = math.lerp(cameraModifier.Position, positionResult, dt * 1.25f);
				            cameraModifier.Position = Vector3.MoveTowards(cameraModifier.Position, positionResult, dt * 0.3f);
				            //cameraModifier.Position.x = positionResult.x;
				            if (math.isnan(cameraModifier.Position.x) || math.abs(cameraModifier.Position.x) > 4000.0f)
				            {
					            cameraModifier.Position.x = 0;
				            }
				            
				            Debug.DrawRay(cameraModifier.Position, Vector3.up * 4, Color.blue);

				            anchor.Type  = AnchorType.Screen;
				            anchor.Value = new float2(0, 0.7f);
			            })
			            .WithReadOnly(directionFromEntity)
			            .Schedule(inputDeps);

			return inputDeps;
		}
	}
}