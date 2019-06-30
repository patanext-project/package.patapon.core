using System;
using System.Collections.Generic;
using System.Linq;
using package.stormiumteam.shared;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

#endif

namespace package.patapon.core
{
    // TODO: UPGRADE
    [Serializable]
    public struct DSplineData : IComponentData
    {
        public float Tension;
        public int   Step;
        public bool  IsLooping;
        
        public float BoundsOutline;
        public EActivationType ActivationType;
    }

    [Serializable]
    public struct DSplineBoundsData : IComponentData
    {
        public float3 Min;
        public float3 Max;
    }

    [InternalBufferCapacity(6)]
    public struct DSplinePoint : IBufferElementData
    {
        public float3 Position;
    }
    
   
    public struct DSplineResult : IBufferElementData
    {
        public float3 Position;
    }

    public struct DSplineValidTag : IComponentData
    {
        
    }

    [RequireComponent(typeof(GameObjectEntity))]
    public class SplineRendererBehaviour : MonoBehaviour
    {
        private GameObjectEntity m_GameObjectEntity;
        
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        // Fields
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        public int   Step    = 6;
        public float Tension = 0.5f;
        public bool  IsLooping;

        public EActivationType RefreshType;
        public float           RefreshBoundsOutline = 1f;

        [SerializeField]
        internal Transform[]  points;

        private bool m_IsDirty;
        
        public LineRenderer LineRenderer;

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
            var referencable = ReferencableGameObject.GetComponent<ReferencableGameObject>(gameObject);
            var goEntity     = referencable.GetOrAddComponent<GameObjectEntity>();

            m_GameObjectEntity = goEntity;
            MarkDirty();
        }

        private void OnEnable()
        {
            var referencable = ReferencableGameObject.GetComponent<ReferencableGameObject>(gameObject);
            var goEntity     = referencable.GetComponentFast<GameObjectEntity>();
            if (!goEntity.HasValue)
            {
                Debug.LogError("No GameObjectEntity found on " + gameObject.name);
            }
            
            
            var e  = goEntity.Value.Entity;
            var em = goEntity.Value.EntityManager;
            if (!em.HasComponent(e, typeof(SplineRendererBehaviour)))
                em.AddComponentObject(e, this);
                
            em.AddComponentData(e, GetData());
            em.AddComponentData(e, new DSplineBoundsData());
            em.AddBuffer<DSplinePoint>(e);
            var r = em.AddBuffer<DSplineResult>(e);
            r.ResizeUninitialized(32); // go outside of the chunk memory

            if (LineRenderer == null)
            {
                Debug.LogWarning("LineRender == null");
                LineRenderer = GetComponentInChildren<LineRenderer>();
            }

            m_GameObjectEntity = goEntity;
            MarkDirty();
            
            // in case we are created in the client worlds...
            var world = m_GameObjectEntity.EntityManager.World;
            if (ClientServerBootstrap.clientWorld.Contains(world))
            {
                var presentationGroup = world.GetExistingSystem<ClientPresentationSystemGroup>();
                presentationGroup.AddSystemToUpdateList(world.GetOrCreateSystem<UpdateSplinePointsSystem>());
                presentationGroup.AddSystemToUpdateList(world.GetOrCreateSystem<SplineSystem>());
            }
        }

        private void OnValidate()
        {
            var goEntity = GetComponent<GameObjectEntity>();
            if (!Application.isPlaying || goEntity?.EntityManager == null)
                return;

            goEntity.EntityManager.SetComponentData(goEntity.Entity, GetData());
            MarkDirty();
        }

        private void Update()
        {
            if (!m_IsDirty)
                return;

            var em = m_GameObjectEntity.EntityManager;
            var e  = m_GameObjectEntity.Entity;

            var canBeProcessed = points.Length > 0
                                 && LineRenderer != null;

            if (canBeProcessed && !em.HasComponent<DSplineValidTag>(e))
            {
                em.AddComponentData(e, new DSplineValidTag());
            }
            else if (!canBeProcessed && em.HasComponent<DSplineValidTag>(e))
            {
                em.RemoveComponent<DSplineValidTag>(e);
            }

            m_IsDirty = false;
        }

        public void MarkDirty()
        {
            m_IsDirty = true;
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
                m_EditorFillerArray = m_EditorFillerArray ?? new List<Vector3>(points.Length * Step);
                m_EditorFillerArray.Clear();
                
                CGraphicalCatmullromSplineUtility.CalculateCatmullromSpline
                (
                    points, 0, points.Length,
                    m_EditorFillerArray, 0,
                    Step, Tension, IsLooping
                );

                if (points.Length > 0 && m_EditorFillerArray.Count > 0)
                {
                    m_EditorFillerArray[0]                             = points[0].localPosition;
                    m_EditorFillerArray[m_EditorFillerArray.Count - 1] = points[points.Length - 1].localPosition;
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

            if (RefreshType == EActivationType.Bounds)
            {
                var boundsMin = new Vector3();
                var boundsMax = new Vector3();
                for (var i = 0; i != points.Length; i++)
                {
                    var point = (float3) points[i].transform.position;

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
            var referencable = ReferencableGameObject.GetComponent<ReferencableGameObject>(gameObject);
            var goEntity     = referencable.GetComponentFast<GameObjectEntity>();
            if (!goEntity.HasValue)
            {
                Debug.LogError("No GameObjectEntity found on " + gameObject.name);
            }
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
                IsLooping = IsLooping
            };
        }

        public Vector3 GetPoint(int i)
        {
            return points[i].position;
        }

        public void SetPoint(int i, Vector3 value)
        {
            points[i].position = value;

            MarkDirty();
        }

        public void SetPointTransforms(params Transform[] transforms)
        {
            points = transforms;
            
            MarkDirty();
        }
    }
}