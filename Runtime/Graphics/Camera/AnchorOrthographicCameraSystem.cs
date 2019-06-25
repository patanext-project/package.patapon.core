using System;
using package.stormiumteam.shared;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace package.patapon.core
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    // Update after animators
    public class AnchorOrthographicCameraSystem : ComponentSystem
    {
        private Entity      m_FirstCamera;
        private EntityQuery m_CameraEntityQuery;

        struct TargetJob : IJobForEach<CameraTargetData, CameraTargetAnchor, CameraTargetPosition>
        {
            public Entity DefaultCamera;

            [DeallocateOnJobCompletion]
            public UnsafeAllocation<int> HighestPriorityAllocation;

            // For foreign entities
            [ReadOnly]
            public ComponentDataFromEntity<AnchorOrthographicCameraData> CameraDataFromEntity;

            [WriteOnly, NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Translation> TranslationFromEntity;

            // Debug
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<AnchorOrthographicCameraOutput> OutputFromEntity;

            public void Execute(ref CameraTargetData data, ref CameraTargetAnchor anchor, ref CameraTargetPosition position)
            {
                ref var highestPriority = ref HighestPriorityAllocation.AsRef();

                // Don't update the camera if it got a lower priority
                if (data.Priority < highestPriority)
                    return;

                // Update the priority
                highestPriority = data.Priority;

                // Update target
                if (data.CameraId != default && !CameraDataFromEntity.Exists(data.CameraId))
                    return;
                var targetCamera = data.CameraId == default ? DefaultCamera : data.CameraId;
                
                var cameraData = CameraDataFromEntity[targetCamera];
                var camSize    = new float2(cameraData.Width, cameraData.Height);
                var anchorPos  = new float2(anchor.Value.x, anchor.Value.y);
                if (anchor.Type == AnchorType.World)
                {
                    // todo: bla bla... world to screen point...
                    throw new NotImplementedException();
                }

                var left = math.float2(1, 0) * (anchorPos.x * camSize.x);
                var up   = math.float2(0, 1) * (anchorPos.y * camSize.y);
                
                TranslationFromEntity[targetCamera] = new Translation
                {
                    Value = math.float3(position.Value.xy + left + up, -100)
                };

                // Debug usage, render an output
                if (!OutputFromEntity.Exists(targetCamera))
                    return;

                OutputFromEntity[targetCamera] = new AnchorOrthographicCameraOutput
                {
                    Target     = position.Value.xy,
                    AnchorType = anchor.Type,
                    Anchor     = anchorPos
                };
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CameraEntityQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<Camera>(),
                    ComponentType.ReadWrite<AnchorOrthographicCameraData>(),
                    ComponentType.ReadWrite<Translation>(),
                }
            });
        }

        protected override void OnUpdate()
        {
            if (m_FirstCamera == default)
            {
                Entities.ForEach((Entity e, GameCamera gameCamera) => { m_FirstCamera = e; });
            }

            // ------------------------------------------------------ //
            // Update camera data
            // ------------------------------------------------------ //
            Entities.With(m_CameraEntityQuery).ForEach((Camera camera, ref AnchorOrthographicCameraData data) =>
            {
                data.Height = camera.orthographicSize;
                data.Width  = camera.aspect * data.Height;
            });

            // ------------------------------------------------------ //
            // Update camera positions from targets
            // ------------------------------------------------------ //
            var jobHandle = new TargetJob
            {
                DefaultCamera             = m_FirstCamera,
                HighestPriorityAllocation = new UnsafeAllocation<int>(Allocator.TempJob, int.MinValue),
                CameraDataFromEntity      = GetComponentDataFromEntity<AnchorOrthographicCameraData>(),
                TranslationFromEntity     = GetComponentDataFromEntity<Translation>(),
                OutputFromEntity          = GetComponentDataFromEntity<AnchorOrthographicCameraOutput>()
            }.Schedule(this);

            jobHandle.Complete();
        }
    }
}