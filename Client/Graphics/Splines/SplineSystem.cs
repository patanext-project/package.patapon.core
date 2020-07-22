using System;
using System.Collections.Generic;
using package.stormiumteam.shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace PataNext.Client.Graphics.Splines
{
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public class UpdateSplinePointsSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.WithAll<DSplineValidTag>().ForEach((SplineRendererBehaviour renderer, DynamicBuffer<DSplinePoint> points) =>
			{
				var transforms = renderer.points;
				var length     = transforms.Length;

				points.ResizeUninitialized(length);
				for (var i = 0; i != length; i++) points[i] = new DSplinePoint {Position = transforms[i].localPosition};
			});
		}
	}

	// TODO: UPGRADE
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[UpdateAfter(typeof(UpdateSplinePointsSystem))]
	public class SplineSystem : SystemBase
	{
		private FastDictionary<int, Vector3[]> ArrayPoolBySize;

		private JobHandle m_LastJobHandle;

		private EntityQuery m_SplineQuery;

		private Dictionary<UnityEngine.Camera, NativeArray<bool>> m_ValidSplinePerCamera;

		// -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
		// Methods
		// -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
		protected override void OnCreate()
		{
			m_SplineQuery = GetEntityQuery
			(
				typeof(DSplineValidTag),
				typeof(DSplinePoint), typeof(DSplineData),
				typeof(DSplineBoundsData),
				typeof(DSplineResult)
			);
			m_ValidSplinePerCamera = new Dictionary<UnityEngine.Camera, NativeArray<bool>>();
			ArrayPoolBySize        = new FastDictionary<int, Vector3[]>();

			RenderPipelineManager.beginFrameRendering  += OnBeginFrameRendering;
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		protected override void OnDestroy()
		{
			m_LastJobHandle.Complete();

			foreach (var kvp in m_ValidSplinePerCamera) kvp.Value.Dispose();

			RenderPipelineManager.beginFrameRendering  -= OnBeginFrameRendering;
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}

		private void OnBeginFrameRendering(ScriptableRenderContext ctx, UnityEngine.Camera[] cameras)
		{
			foreach (var previous in m_ValidSplinePerCamera) previous.Value.Dispose();
			m_ValidSplinePerCamera.Clear();

			for (var cam = 0; cam != cameras.Length; cam++)
			{
				m_ValidSplinePerCamera[cameras[cam]] = new NativeArray<bool>(m_SplineQuery.CalculateEntityCount(), Allocator.TempJob);

				var bounds = new Bounds(cameras[cam].transform.position, cameras[cam].GetExtents()).Flat2D();
				m_LastJobHandle = new JobIntersection
				{
					CameraBounds = bounds,
					ValidSplines = m_ValidSplinePerCamera[cameras[cam]]
				}.Schedule(m_SplineQuery, m_LastJobHandle);
			}
		}

		private unsafe void OnBeginCameraRendering(ScriptableRenderContext ctx, UnityEngine.Camera cam)
		{
			//< -------- -------- -------- -------- -------- -------- -------- ------- //
			// Finish the current job
			//> -------- -------- -------- -------- -------- -------- -------- ------- //
			m_LastJobHandle.Complete();

			var previousCount = -1;
			var array         = default(Vector3[]);
			var validSplines  = m_ValidSplinePerCamera[cam];
			var entities      = m_SplineQuery.ToEntityArray(Allocator.TempJob);

			Profiler.BeginSample("Loop");
			DSplineData                  spline;
			DynamicBuffer<DSplineResult> result;
			SplineRendererBehaviour      renderer;
			int                          resultCount;
			for (int i = 0, length = entities.Length; i < length; i++)
			{
				spline = EntityManager.GetComponentData<DSplineData>(entities[i]);
				if (spline.ActivationType == EActivationType.Bounds && !validSplines[i])
					// ignore spline...
					continue;

				result   = EntityManager.GetBuffer<DSplineResult>(entities[i]);
				renderer = EntityManager.GetComponentObject<SplineRendererBehaviour>(entities[i]);

				resultCount = result.Length;
				if (renderer.LastLineRendererPositionCount != resultCount)
				{
					foreach (var lr in renderer.lineRendererArray) lr.positionCount = resultCount;

					renderer.LastLineRendererPositionCount = resultCount;
				}

				if (previousCount != resultCount)
				{
					previousCount = resultCount;
					if (!ArrayPoolBySize.RefFastTryGet(resultCount, ref array)) ArrayPoolBySize[resultCount] = array = new Vector3[resultCount];
				}

				if (array == null)
					continue;

				fixed (void* buffer = array)
				{
					UnsafeUtility.MemCpy(buffer, result.GetUnsafePtr(), resultCount * sizeof(float3));
				}

				var scaling = math.abs(renderer.transform.lossyScale.x);
				scaling *= renderer.CurveMultiplier;

				foreach (var lr in renderer.lineRendererArray)
				{
					if (!renderer.KeepCurveScaling)
						lr.widthMultiplier = scaling;
					lr.SetPositions(array);
				}
			}

			Profiler.EndSample();

			entities.Dispose();
		}

		protected override void OnUpdate()
		{
			Dependency = m_LastJobHandle = new JobGetResult
			{
				PointsFromEntity = GetBufferFromEntity<DSplinePoint>(true),
				ResultFromEntity = GetBufferFromEntity<DSplineResult>()
			}.Schedule(m_SplineQuery, Dependency);
		}

		[BurstCompile]
		[RequireComponentTag(typeof(DSplineValidTag))]
		private struct JobGetResult : IJobForEachWithEntity<DSplineData, DSplineBoundsData>
		{
			[ReadOnly]
			public BufferFromEntity<DSplinePoint> PointsFromEntity;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<DSplineResult> ResultFromEntity;

			[BurstDiscard]
			private void ExplicitException(in Entity ent, in DSplineData spline, in int pointLength)
			{
				throw new InvalidOperationException($"predicted length was less than 0. {ent} -> step={spline.Step} length={pointLength}");
			}

			public void Execute(Entity entity, int index, ref DSplineData spline, ref DSplineBoundsData bounds)
			{
				var points = PointsFromEntity[entity];
				if (points.Length <= 0)
					return;

				for (var p = 0; p != points.Length; p++)
				{
					var pointPosition = points[p].Position;
					if (p == 0)
					{
						bounds.Min = pointPosition;
						bounds.Max = pointPosition;
					}

					bounds.Min = math.min(pointPosition, bounds.Min);
					bounds.Max = math.max(pointPosition, bounds.Max);
				}

				var predictedLength = CGraphicalCatmullromSplineUtility.GetResultLength(spline.Step, points.Length);
				if (predictedLength < 0)
				{
					ExplicitException(in entity, in spline, points.Length);
					throw new InvalidOperationException("predicted length was less than 0");
				}

				var result = ResultFromEntity[entity];
				result.Capacity = predictedLength + 1;
				result.Clear();

				result.Add(new DSplineResult {Position = points[0].Position});
				CGraphicalCatmullromSplineUtility.CalculateCatmullromSpline
				(
					// points
					points.Reinterpret<float3>(), 0, points.Length,
					// result
					result.Reinterpret<float3>(),
					// settings
					spline.Step, spline.Tension, spline.IsLooping
				);
			}
		}

		[BurstCompile]
		[RequireComponentTag(typeof(DSplineValidTag))]
		private struct JobIntersection : IJobForEachWithEntity<DSplineData, DSplineBoundsData>
		{
			[ReadOnly]
			public Bounds CameraBounds;

			[WriteOnly]
			public NativeArray<bool> ValidSplines;

			public void Execute(Entity entity, int index, ref DSplineData spline, ref DSplineBoundsData bounds)
			{
				var cb = CameraBounds;

				bounds.Min -= spline.BoundsOutline;
				bounds.Max += spline.BoundsOutline;

				var boolean = cb.min.x <= bounds.Max.x && cb.max.x >= bounds.Min.x
				                                       && cb.min.y <= bounds.Max.y && cb.max.y >= bounds.Min.y;

				ValidSplines[index] = boolean;
			}
		}
	}
}