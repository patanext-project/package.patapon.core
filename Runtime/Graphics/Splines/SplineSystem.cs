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
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace package.patapon.core
{
    // TODO: UPGRADE
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class SplineSystem : JobComponentSystem
    {
        private JobHandle           m_LastJobHandle;

        private Dictionary<Camera, NativeArray<bool>> m_ValidSplinePerCamera;
        private FastDictionary<int, Vector3[]> ArrayPoolBySize;
        
        private EntityQuery m_SplineQuery;

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
            m_ValidSplinePerCamera = new Dictionary<Camera, NativeArray<bool>>();
            ArrayPoolBySize = new FastDictionary<int, Vector3[]>();
            
            RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        protected override void OnDestroy()
        {
            m_LastJobHandle.Complete();

            foreach (var kvp in m_ValidSplinePerCamera)
            {
                kvp.Value.Dispose();
            }

            RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        private void OnBeginFrameRendering(ScriptableRenderContext ctx, Camera[] cameras)
        {
            foreach (var previous in m_ValidSplinePerCamera)
            {
                previous.Value.Dispose();
            }
            m_ValidSplinePerCamera.Clear();

            for (var cam = 0; cam != cameras.Length; cam++)
            {
                m_ValidSplinePerCamera[cameras[cam]] = new NativeArray<bool>(m_SplineQuery.CalculateLength(), Allocator.TempJob);

                var bounds = new Bounds(cameras[cam].transform.position, cameras[cam].GetExtents()).Flat2D();
                m_LastJobHandle = new JobIntersection
                {
                    CameraBounds = bounds,
                    ValidSplines = m_ValidSplinePerCamera[cameras[cam]]
                }.Schedule(m_SplineQuery, m_LastJobHandle);
            }
        }

        private unsafe void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
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
            for (var i = 0; i < entities.Length; i++)
            {
                var spline = EntityManager.GetComponentData<DSplineData>(entities[i]);
                if (spline.ActivationType == EActivationType.Bounds && !validSplines[i])
                {
                    // ignore spline...
                    continue;
                }

                var result   = EntityManager.GetBuffer<DSplineResult>(entities[i]);
                var renderer = EntityManager.GetComponentObject<SplineRendererBehaviour>(entities[i]);

                var resultCount = result.Length;
                if (renderer.LastLineRendererPositionCount != resultCount)
                {
                    renderer.LineRenderer.positionCount    = resultCount;
                    renderer.LastLineRendererPositionCount = resultCount;
                }

                if (previousCount != resultCount)
                {
                    previousCount = resultCount;
                    if (!ArrayPoolBySize.RefFastTryGet(resultCount, ref array))
                    {
                        ArrayPoolBySize[resultCount] = new Vector3[resultCount];
                    }
                }

                fixed (void* buffer = array)
                {
                    UnsafeUtility.MemCpy(buffer, result.GetUnsafePtr(), resultCount * sizeof(float3));
                }

                renderer.LineRenderer.SetPositions(array);
            }

            Profiler.EndSample();

            entities.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_LastJobHandle = new JobGetResult
            {
                PointsFromEntity = GetBufferFromEntity<DSplinePoint>(true),
                ResultFromEntity = GetBufferFromEntity<DSplineResult>()
            }.Schedule(m_SplineQuery, inputDeps);

            return m_LastJobHandle;
        }

        [BurstCompile]
        [RequireComponentTag(typeof(DSplineValidTag))]
        private struct JobGetResult : IJobForEachWithEntity<DSplineData, DSplineBoundsData>
        {
            [ReadOnly]
            public BufferFromEntity<DSplinePoint> PointsFromEntity;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<DSplineResult> ResultFromEntity;

            public void Execute(Entity entity, int index, ref DSplineData spline, ref DSplineBoundsData bounds)
            {
                var points = PointsFromEntity[entity];
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
                var result          = ResultFromEntity[entity];
                result.Reserve(predictedLength);
                result.Clear();

                result.Add(new DSplineResult{Position = points[0].Position});
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