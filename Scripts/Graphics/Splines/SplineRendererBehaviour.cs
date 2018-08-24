using System;
using System.Collections.Generic;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace P4.Core.Graphics
{
    [Serializable]
    public struct DSplineData : IComponentData
    {
        public float Tension;
        public int   Step;
        public int   IsLooping;
    }

    [Serializable]
    public struct DSplineBoundsData : IComponentData
    {
        public float2 Min;
        public float2 Max;
    }

    public class SplineRendererBehaviour : MonoBehaviour
    {
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        // Fields
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        public int   Step    = 6;
        public float Tension = 0.5f;
        public bool  IsLooping;

        public EActivationZone RefreshType;
        public float           RefreshBoundsOutline = 1f;

        public Transform[]  Points;
        public LineRenderer LineRenderer;

        private int m_CurrentPointsLength;
        
        #if UNITY_EDITOR
        private List<Vector3> m_EditorFillerArray;
        private Vector3[] m_EditorResultArray;
        #endif
        
        internal int LastLineRendererPositionCount;
        internal int CameraRenderCount;

        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        // Unity Methods
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        private void Awake()
        {
            var goEntity = GetComponent<GameObjectEntity>();
            var e        = goEntity.Entity;
            var em       = goEntity.EntityManager;

            em.AddComponentData(e, GetData());
            em.AddComponentData(e, new DSplineBoundsData());

            m_CurrentPointsLength = Points.Length;

            World.Active.GetExistingManager<SplineSystem>().SendUpdateEvent(goEntity.Entity);
        }

        private void OnValidate()
        {
            var goEntity = GetComponent<GameObjectEntity>();
            if (!Application.isPlaying || goEntity?.EntityManager == null)
                return;
            if (Points.Length != m_CurrentPointsLength)
            {
                Debug.LogError("Can't add/remove points of a spline while the gameobject is active!");
                return;
            }

            goEntity.EntityManager.SetComponentData(goEntity.Entity, GetData());

            World.Active.GetExistingManager<SplineSystem>().SendUpdateEvent(goEntity.Entity);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var currCam = Camera.current;

            var isSelected = Selection.activeGameObject == gameObject;
            Gizmos.color = isSelected ? Color.blue : Color.magenta;

            if (!Application.isPlaying
                && LineRenderer != null)
            {
                // Render spline
                m_EditorFillerArray = m_EditorFillerArray ?? new List<Vector3>(Points.Length * Step);
                m_EditorFillerArray.Clear();
                
                CGraphicalCatmullromSplineUtility.CalculateCatmullromSpline
                (
                    Points, 0, Points.Length,
                    m_EditorFillerArray, 0,
                    Step, Tension, IsLooping
                );

                if (Points.Length > 0 && m_EditorFillerArray.Count > 0)
                {
                    m_EditorFillerArray[0]                             = Points[0].localPosition;
                    m_EditorFillerArray[m_EditorFillerArray.Count - 1] = Points[Points.Length - 1].localPosition;
                }

                if (m_EditorResultArray == null || m_EditorResultArray.Length != m_EditorFillerArray.Count)
                {
                    m_EditorResultArray = new Vector3[m_EditorFillerArray.Count];
                }
                
                for (int i = 0; i != m_EditorResultArray.Length; i++)
                {
                    m_EditorResultArray[i] = m_EditorFillerArray[i];
                }

                LineRenderer.positionCount = m_EditorResultArray.Length;
                LineRenderer.SetPositions(m_EditorResultArray);
            }

            if (RefreshType == EActivationZone.Bounds)
            {
                var boundsMin = new Vector3();
                var boundsMax = new Vector3();
                for (var i = 0; i != Points.Length; i++)
                {
                    var point = (float3) Points[i].transform.position;

                    if (i == 0)
                    {
                        boundsMin = point;
                        boundsMax = point;
                    }

                    var min = boundsMin;
                    var max = boundsMax;
                    boundsMin = math.min(point, min);
                    boundsMax = math.max(point, max);
                }

                boundsMin.x -= RefreshBoundsOutline;
                boundsMin.y -= RefreshBoundsOutline;
                boundsMax.x += RefreshBoundsOutline;
                boundsMax.y += RefreshBoundsOutline;

                var bounds = new Bounds();
                bounds.SetMinMax(boundsMin, boundsMax);

                var camBounds = new Bounds(currCam.transform.position, currCam.GetExtents()).Flat2D();
                if (camBounds.Intersects(bounds))
                    Gizmos.color = isSelected ? new Color(0.5f, 0.75f, 0.35f) : Color.green;
                else
                    Gizmos.color = isSelected ? new Color(0.75f, 0.35f, 0.5f) : Color.red;

                Gizmos.DrawWireCube(camBounds.center, camBounds.size);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
#endif

        private void OnDisable()
        {
            var goEntity = GetComponent<GameObjectEntity>();
            World.Active?.GetExistingManager<SplineSystem>().SendUpdateEvent(goEntity.Entity);
        }

        private void OnDestroy()
        {
            var goEntity = GetComponent<GameObjectEntity>();
            World.Active?.GetExistingManager<SplineSystem>().SendUpdateEvent(goEntity.Entity);
        }

        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        // Methods
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        public DSplineData GetData()
        {
            return new DSplineData
            {
                Step      = Step,
                Tension   = Tension,
                IsLooping = IsLooping ? 1 : 0
            };
        }

        public Vector3 GetPoint(int i)
        {
            return Points[i].position;
        }

        public void SetPoint(int i, Vector3 value)
        {
            Points[i].position = value;
        }
    }
}