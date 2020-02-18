using System;
using System.Collections.Generic;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;

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

		public float           BoundsOutline;
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

	public class SplineRendererBehaviour : MonoBehaviour
	{
		internal int  CameraRenderCount;
		public   bool IsLooping;
		public float CurveMultiplier = 1;
		public bool KeepCurveScaling = true;

		internal int LastLineRendererPositionCount;

		[SerializeField] [FormerlySerializedAs("LineRenderer")]
		private LineRenderer lineRenderer;

		public  LineRenderer[] lineRendererArray;
		private Entity         m_Entity;
		private EntityManager  m_EntityManager;

		private bool m_IsDirty;

		[SerializeField]
		internal Transform[] points;

		public float RefreshBoundsOutline = 1f;

		public EActivationType RefreshType;

		// -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
		// Fields
		// -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
		public int   Step    = 6;
		public float Tension = 0.5f;

		// -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
		// Unity Methods
		// -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
		private void Awake()
		{
			MarkDirty();
		}

		private void OnEnable()
		{
			if (lineRenderer != null) lineRendererArray = new[] {lineRenderer};

			m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
			m_Entity        = m_EntityManager.CreateEntity();

			if (!m_EntityManager.HasComponent(m_Entity, typeof(SplineRendererBehaviour)))
				m_EntityManager.AddComponentObject(m_Entity, this);

			m_EntityManager.AddComponentData(m_Entity, GetData());
			m_EntityManager.AddComponentData(m_Entity, new DSplineBoundsData());
			m_EntityManager.AddBuffer<DSplinePoint>(m_Entity);
			var r = m_EntityManager.AddBuffer<DSplineResult>(m_Entity);
			r.ResizeUninitialized(r.Capacity + 1); // go outside of the chunk memory

			if (lineRendererArray == null)
			{
				Debug.LogWarning("LineRender == null");
				lineRendererArray = GetComponentsInChildren<LineRenderer>();
			}

			MarkDirty();
		}

		private void OnValidate()
		{
			IsLooping = false;
			
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

			var canBeProcessed = points.Length > 0
			                     && lineRendererArray != null;

			if (canBeProcessed && !m_EntityManager.HasComponent<DSplineValidTag>(m_Entity))
				m_EntityManager.AddComponentData(m_Entity, new DSplineValidTag());
			else if (!canBeProcessed && m_EntityManager.HasComponent<DSplineValidTag>(m_Entity)) m_EntityManager.RemoveComponent<DSplineValidTag>(m_Entity);

			m_IsDirty = false;
		}

		public void MarkDirty()
		{
			m_IsDirty = true;
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			IsLooping = false;
			
			if (lineRendererArray == null || lineRendererArray.Length == 0) lineRendererArray = new[] {lineRenderer};

			var currCam = Camera.current;

			var isSelected = Selection.activeGameObject == gameObject;
			Gizmos.color = isSelected ? Color.blue : Color.magenta;

			if (!Application.isPlaying
			    && lineRendererArray != null)
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

				if (m_EditorResultArray == null || m_EditorResultArray.Length != m_EditorFillerArray.Count) m_EditorResultArray = new Vector3[m_EditorFillerArray.Count];

				for (var i = 0; i != m_EditorResultArray.Length; i++) m_EditorResultArray[i] = m_EditorFillerArray[i];

				foreach (var lr in lineRendererArray)
				{
					lr.positionCount = m_EditorResultArray.Length;
					lr.SetPositions(m_EditorResultArray);

					if (!KeepCurveScaling)
						lr.widthMultiplier = math.abs(transform.lossyScale.x * CurveMultiplier);
				}
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
			void Pop()
			{
				// kinda weird to do a check on World, but there were some problems with it
				if (m_EntityManager?.World == null)
					return;

				if (!m_EntityManager.Exists(m_Entity))
					return;

				m_EntityManager.DestroyEntity(m_Entity);
			}

			Pop();
			m_EntityManager = null;
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

#if UNITY_EDITOR
		private List<Vector3> m_EditorFillerArray;
		private Vector3[]     m_EditorResultArray;
#endif
	}
}