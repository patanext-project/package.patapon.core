using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace package.patapon.core
{    
    public struct AnchorOrthographicCameraData : IComponentData
    {
        public float Height, Width;
        
        public AnchorOrthographicCameraData(float height, float width)
        {
            Height = height;
            Width = width;
        }
    }

    public struct AnchorOrthographicCameraOutput : IComponentData
    {
        public float2 Target;
        public AnchorType AnchorType;
        public float2 Anchor;
    }
    
    [RequireComponent(typeof(GameObjectEntity), typeof(Camera))]
    [ExecuteInEditMode]
    public class AnchorOrthographicCameraBehaviour : MonoBehaviour
    {
        private Camera m_Camera;
        private GameObjectEntity m_GameObjectEntity;

        [SerializeField] private float2 m_DebugTarget;

        private void OnEnable()
        {
            m_Camera           = GetComponent<Camera>();
            m_GameObjectEntity = GetComponent<GameObjectEntity>();
            
            RefreshData();
        }

        private void OnValidate()
        {
            m_Camera           = GetComponent<Camera>();
            m_GameObjectEntity = GetComponent<GameObjectEntity>();
            
            RefreshData(true);
        }

        private void RefreshData(bool setDebugData = false)
        {
            var height = m_Camera.orthographicSize;
            var width  = m_Camera.aspect * height;
            
            var e = m_GameObjectEntity.Entity;
            if (e == Entity.Null)
                return;

            e.SetOrAddComponentData(new AnchorOrthographicCameraData(height, width));
            e.SetOrAddComponentData(new Translation { Value = transform.position });
            e.SetOrAddComponentData(new Rotation { Value = transform.rotation });
            e.SetOrAddComponentData(new LocalToWorld());
            e.SetOrAddComponentData(new AnchorOrthographicCameraOutput());

            if (setDebugData)
            {
                e.SetComponentData(new Translation {Value = new float3(m_DebugTarget.x, m_DebugTarget.y, -100)});
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var e = m_GameObjectEntity.Entity;
            if (!e.HasComponent<AnchorOrthographicCameraOutput>())
                return;

            var output = e.GetComponentData<AnchorOrthographicCameraOutput>();
                
            var height = m_Camera.orthographicSize;
            var width  = m_Camera.aspect * height;

            var up      = Vector3.up * (output.Anchor.y * height);
            var left    = Vector3.left * (output.Anchor.x * width);
            var posUp   = transform.position - up;
            var posLeft = transform.position - left;

            // Draw target sphere...
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(output.Target.x, output.Target.y, 0), 0.25f);

            // Draw rect 
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(posUp, 0.25f);
            Gizmos.DrawWireSphere(posLeft, 0.25f);

            // Draw lines
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(posUp - new Vector3(width, 0, 0), posUp + new Vector3(width, 0, 0));
            Gizmos.DrawLine(posLeft - new Vector3(0, height, 0), posLeft + new Vector3(0, height, 0));
        }
#endif
    }
}