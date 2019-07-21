using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

namespace package.patapon.core
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class UpdateUnityCameraSystem : ComponentSystem
    {
        struct DataToSet
        {
            public CameraMode LastSuperiorMode;

            public Entity     Target;
            public float3     PosOffset;
            public quaternion RotOffset;
        }

        struct CameraData
        {
            public Camera Camera;
            public Entity Entity;
        }

        private DataToSet  m_DataToSet;
        private CameraData m_CameraData;

        protected override void OnUpdate()
        {
            m_DataToSet  = default;
            m_CameraData = default;

            Profiler.BeginSample("ForEach1");
            Entities.ForEach((Entity entity, GameCamera gameCamera) =>
            {
                if (m_CameraData.Entity != default)
                {
                    Debug.LogWarning($"There is already a game camera, but we found another one? (c={m_CameraData.Entity}, n={entity})");
                    return;
                }

                m_CameraData.Camera = gameCamera.Camera;
                m_CameraData.Entity = entity;
            });
            Profiler.EndSample();

            Profiler.BeginSample("ForEach2");
            Entities.ForEach((ref LocalCameraState cameraState) =>
            {
                if (m_DataToSet.LastSuperiorMode > cameraState.Mode)
                    return;

                m_DataToSet.LastSuperiorMode = cameraState.Mode;
                m_DataToSet.Target           = cameraState.Target;
                m_DataToSet.PosOffset        = cameraState.Offset.pos;
                m_DataToSet.RotOffset        = cameraState.Offset.rot;
            });
            Profiler.EndSample();

            Profiler.BeginSample("ForEach3");
            Entities.ForEach((ref GamePlayer player, ref ServerCameraState cameraState) =>
            {
                if (!player.IsSelf)
                    return;

                if (m_DataToSet.LastSuperiorMode > cameraState.Mode)
                    return;

                m_DataToSet.LastSuperiorMode = cameraState.Mode;
                m_DataToSet.Target           = cameraState.Target;
                m_DataToSet.PosOffset        = cameraState.Offset.pos;
                m_DataToSet.RotOffset        = cameraState.Offset.rot;
            });
            Profiler.EndSample();

            if (m_CameraData.Entity == default)
            {
                Debug.LogError("No Game Camera found?");
                return;
            }

            Compute(m_CameraData.Camera);
        }

        private void Compute(Camera camera)
        {
            if (m_DataToSet.Target == default)
            {
                return;
            }

            if (!math.all(m_DataToSet.RotOffset.value))
                m_DataToSet.RotOffset = quaternion.identity;

            var modifier = EntityManager.GetComponentData<CameraModifierData>(m_DataToSet.Target);
            var tr       = camera.transform;

            camera.fieldOfView = math.max(modifier.FieldOfView, 30);

            tr.position = modifier.Position + m_DataToSet.PosOffset;
            tr.rotation = math.mul(modifier.Rotation, m_DataToSet.RotOffset);
        }
    }
}