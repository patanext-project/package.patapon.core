using System;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace package.patapon.core
{
    [UpdateBefore(typeof(CopyTransformToGameObjectSystem))] // We need to force "CopyTransformToGameObject" to be performed in PreLateUpdate
    [UpdateAfter(typeof(PreLateUpdate.DirectorUpdateAnimationEnd))] // Update after animators
    public class AnchorOrthographicCameraSystem : ComponentSystem
    {
        /// <summary>
        /// The target group that the camera will use
        /// </summary>
        public struct TargetGroup
        {
            public ComponentDataArray<CameraTargetData>     Data;
            public ComponentDataArray<CameraTargetAnchor>   AnchorData;
            public ComponentDataArray<CameraTargetPosition> PositionData;

            public SubtractiveComponent<VoidSystem<AnchorOrthographicCameraSystem>> Void1;

            public readonly int Length;
        }

        [Inject] private TargetGroup m_TargetGroup;

        /// <summary>
        /// The camera group
        /// </summary>
        public struct CameraGroup
        {
            public ComponentArray<Camera> Cameras;
            public ComponentDataArray<AnchorOrthographicCameraData> Data;
            public ComponentDataArray<Position>                     Positions;
            public EntityArray                                      Entities;

            public SubtractiveComponent<VoidSystem<AnchorOrthographicCameraSystem>> Void1;

            public readonly int Length;
        }

        [Inject] private CameraGroup m_CameraGroup;

        protected override void OnUpdate()
        {
            // ------------------------------------------------------ //
            // Update camera data
            // ------------------------------------------------------ //
            for (int i = 0; i != m_CameraGroup.Length; i++)
            {
                // Get components
                var camera = m_CameraGroup.Cameras[i];
                
                var height = camera.orthographicSize;
                var width  = camera.aspect * height;

                // Update data with the correct height and width
                m_CameraGroup.Data[i] = new AnchorOrthographicCameraData(height, width);
            }
            
            // Update injected groups
            UpdateInjectedComponentGroups();
            
            // ------------------------------------------------------ //
            // Update camera positions from targets
            // ------------------------------------------------------ //
            for (int i = 0, highestPriority = int.MinValue; i != m_TargetGroup.Length; i++)
            {
                // Get components
                var data     = m_TargetGroup.Data[i];
                var anchor   = m_TargetGroup.AnchorData[i];
                var position = m_TargetGroup.PositionData[i];

                // Don't update the camera if it got a lower priority
                if (data.Priority < highestPriority)
                    continue;

                // Update the priority
                data.Priority = highestPriority;

                // Update target
                UpdateTarget(data, anchor, position.Value);
            }
        }

        private void UpdateTarget(CameraTargetData data, CameraTargetAnchor anchor, float2 targetPosition)
        {
            Entity                       entity     = default;
            AnchorOrthographicCameraData cameraData = default;
            
            // Verify the camera target is correct
            for (int i = 0; i != m_CameraGroup.Length; i++)
            {
                if (m_CameraGroup.Entities[i] != data.CameraId) continue;

                entity     = m_CameraGroup.Entities[i];
                cameraData = m_CameraGroup.Data[i];
            }

            if (entity == Entity.Null) return;

            var camSize = new float2(cameraData.Width, cameraData.Height);
            var anchorPos = new float2(anchor.Value.x, anchor.Value.y);
            if (anchor.Type == AnchorType.World)
            {
                // todo: bla bla... world to screen point...
                throw new NotImplementedException();
            }

            var left = math.float2(1, 0) * (anchorPos.x * camSize.x);
            var up   = math.float2(0, 1) * (anchorPos.y * camSize.y);

            entity.SetComponentData(new Position {Value = math.float3(targetPosition + left + up, -100)});
            
            // Debug usage, render an output
            if (entity.HasComponent<AnchorOrthographicCameraOutput>())
            {
                entity.SetComponentData
                (
                    new AnchorOrthographicCameraOutput
                    {
                        Target     = targetPosition,
                        AnchorType = anchor.Type,
                        Anchor     = anchorPos
                    }
                );
            }
        }
    }
}