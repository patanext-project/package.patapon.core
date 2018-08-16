using System;
using package.stormiumteam.shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Jobs;

namespace package.patapon.core
{
    [UpdateBefore(typeof(CopyTransformToGameObjectSystem))]
    [UpdateAfter(typeof(PreLateUpdate.DirectorUpdateAnimationEnd))]
    public class AnchorOrthographicCameraSystem : ComponentSystem
    {
        public struct TargetGroup
        {
            public ComponentDataArray<CameraTargetData>     Data;
            public ComponentDataArray<CameraTargetAnchor>   AnchorData;
            public ComponentDataArray<CameraTargetPosition> PositionData;

            public SubtractiveComponent<VoidSystem<AnchorOrthographicCameraSystem>> Void1;

            public readonly int Length;
        }

        [Inject] private TargetGroup m_TargetGroup;

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
            for (int i = 0; i != m_CameraGroup.Length; i++)
            {
                var camera = m_CameraGroup.Cameras[i];
                
                var height = camera.orthographicSize;
                var width  = camera.aspect * height;

                m_CameraGroup.Data[i] = new AnchorOrthographicCameraData(height, width);
            }
            
            UpdateInjectedComponentGroups();
            
            var highestPriority = int.MinValue;
            for (int i = 0; i != m_TargetGroup.Length; i++)
            {
                var data     = m_TargetGroup.Data[i];
                var anchor   = m_TargetGroup.AnchorData[i];
                var position = m_TargetGroup.PositionData[i];

                if (data.Priority < highestPriority)
                    continue;

                data.Priority = highestPriority;

                UpdateTarget(data, anchor, position.Value);
            }
        }

        private void UpdateTarget(CameraTargetData data, CameraTargetAnchor anchor, float2 targetPosition)
        {
            Entity                       entity     = default;
            AnchorOrthographicCameraData cameraData = default;
            
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

            entity.SetComponentData(new Position(new float3(targetPosition + left + up, -100)));
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