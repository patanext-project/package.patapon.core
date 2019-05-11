using System;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace package.patapon.core
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    // Update after animators
    public class AnchorOrthographicCameraSystem : ComponentSystem
    {
        private EntityQuery m_CameraComponentGroup;

        struct TargetJob : IJobProcessComponentData<CameraTargetData, CameraTargetAnchor, CameraTargetPosition>
        {
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
                if (data.CameraId == default || !CameraDataFromEntity.Exists(data.CameraId))
                    return;

                var cameraData = CameraDataFromEntity[data.CameraId];
                var camSize    = new float2(cameraData.Width, cameraData.Height);
                var anchorPos  = new float2(anchor.Value.x, anchor.Value.y);
                if (anchor.Type == AnchorType.World)
                {
                    // todo: bla bla... world to screen point...
                    throw new NotImplementedException();
                }

                var left = math.float2(1, 0) * (anchorPos.x * camSize.x);
                var up   = math.float2(0, 1) * (anchorPos.y * camSize.y);

                TranslationFromEntity[data.CameraId] = new Translation
                {
                    Value = math.float3(position.Value.xy + left + up, -100)
                };

                // Debug usage, render an output
                if (!OutputFromEntity.Exists(data.CameraId))
                    return;

                OutputFromEntity[data.CameraId] = new AnchorOrthographicCameraOutput
                {
                    Target     = position.Value.xy,
                    AnchorType = anchor.Type,
                    Anchor     = anchorPos
                };
            }
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_CameraComponentGroup = GetEntityQuery(new EntityQueryDesc
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
            // ------------------------------------------------------ //
            // Update camera data
            // ------------------------------------------------------ //
            ForEach((Camera camera, ref AnchorOrthographicCameraData data) =>
            {
                data.Height = camera.orthographicSize;
                data.Width  = camera.aspect * data.Height;
            }, m_CameraComponentGroup);
            
            // ------------------------------------------------------ //
            // Update camera positions from targets
            // ------------------------------------------------------ //
            var jobHandle = new TargetJob
            {
                HighestPriorityAllocation = new UnsafeAllocation<int>(Allocator.TempJob, int.MinValue),
                CameraDataFromEntity      = GetComponentDataFromEntity<AnchorOrthographicCameraData>(),
                TranslationFromEntity     = GetComponentDataFromEntity<Translation>(),
                OutputFromEntity          = GetComponentDataFromEntity<AnchorOrthographicCameraOutput>()
            }.Schedule(this);
            
            jobHandle.Complete();
        }
    }
}