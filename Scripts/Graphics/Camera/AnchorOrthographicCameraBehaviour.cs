using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace package.patapon.core
{
    public struct AnchorOrthographicCameraTarget : IComponentData
    {
        public float3 Target;

        public AnchorOrthographicCameraTarget(float3 target)
        {
            Target = target;
        }
    }
    
    public struct AnchorOrthographicCameraData : IComponentData
    {
        public float2 Anchor;
        public float Height, Width;
        
        public AnchorOrthographicCameraData(float2 anchor, float height, float width)
        {
            Anchor = anchor;
            Height = height;
            Width = width;
        }
    }
    
    [RequireComponent(typeof(GameObjectEntity), typeof(Camera))]
    [ExecuteInEditMode]
    public class AnchorOrthographicCameraBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Vector3 m_Target;

        [Range(-1, 1)]
        public float AnchorX, AnchorY;

        private Camera m_Camera;
        private GameObjectEntity m_GameObjectEntity;

        public void SetAnchor(Vector2 anchor)
        {
            AnchorX = Mathf.Clamp(anchor.x, -1, 1);
            AnchorY = Mathf.Clamp(anchor.y, -1, 1);
            
            RefreshData();
        }

        public Vector2 GetAnchor()
        {
            return new Vector2(AnchorX, AnchorY);
        }
        
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
            
            RefreshData();
        }

        private void RefreshData()
        {
            var height = m_Camera.orthographicSize;
            var width  = m_Camera.aspect * height;
            
            var e = m_GameObjectEntity.Entity;
            if (e == Entity.Null)
                return;
            
            e.SetOrAddComponentData(new AnchorOrthographicCameraTarget(m_Target));
            e.SetOrAddComponentData(new AnchorOrthographicCameraData(math.float2(AnchorX, AnchorY), height, width));
            e.SetOrAddComponentData(new Position(transform.position));
            e.SetOrAddComponentData(new CopyTransformToGameObject());
        }

        private void Update()
        {
            SetAnchor(new Vector2(Mathf.Sin(Time.time), Mathf.Cos(Time.time)));
            
            if (!m_Camera.orthographic || Application.isPlaying)
                return;

            var height = m_Camera.orthographicSize;
            var width  = m_Camera.aspect * height;

            var up   = Vector3.up * (AnchorY * height);
            var left = Vector3.left * (AnchorX * width);

            transform.position = m_Target + left + up;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var height = m_Camera.orthographicSize;
            var width  = m_Camera.aspect * height;

            var up      = Vector3.up * (AnchorY * height);
            var left    = Vector3.left * (AnchorX * width);
            var posUp   = transform.position - up;
            var posLeft = transform.position - left;

            // Draw target sphere...
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(m_Target, 0.25f);

            // Draw rect 
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(posUp, 0.25f);
            Gizmos.DrawWireSphere(posLeft, 0.25f);

            // Draw lines
            Gizmos.color = Color.white;
            Gizmos.DrawLine(posUp - new Vector3(width, 0, 0), posUp + new Vector3(width, 0, 0));
            Gizmos.DrawLine(posLeft - new Vector3(0, height, 0), posLeft + new Vector3(0, height, 0));
        }
#endif
    }
}