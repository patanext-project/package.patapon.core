using System;
using package.stormiumteam.shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace package.patapon.core
{
    [UpdateAfter(typeof(PreLateUpdate.DirectorUpdateAnimationEnd))]
    //< Update after the 'LateUpdate', so all animations can be finished 
    public class SplineSystem : JobComponentSystem
    {
        private          JobHandle           LastJobHandle;
        private          EntityArchetype     m_EventArchetype;
        private          int                 m_Events;
        private          NativeArray<float3> m_FinalFillerArray;

        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        // Fields
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        [Inject] private Group m_Group;

        private NativeArray<DSplineBoundsData> m_JobBoundsDatas;
        private NativeArray<float>             m_JobBoundsOutline;
        private NativeArray<DSplineData>       m_JobDatas;
        private NativeArray<int>               m_jobFormulaAddLength;

        private TransformAccessArray         m_OrderedPoints;
        private int                          m_PointsLength;
        private NativeArray<EActivationZone> m_refreshTypes;

        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        // Methods
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.

        // TODO: Use Reactive Systems instead (but the curren method seems more performant, need to be decided)
        public void SendUpdateEvent(Entity entity)
        {
            /*var eventEntity = EntityManager.CreateEntity(m_EventArchetype);
            EntityManager.SetComponentData(eventEntity, new InitEvent {Id = entity});*/ // It's a lot bugged for now
            m_Events++;
        }

        protected override void OnCreateManager()
        {
            // TODO: Use NativeEvent from the shared package, much more future proof
            Camera.onPreCull   += OnCameraPreCull;
            m_FinalFillerArray =  new NativeArray<float3>(0, Allocator.Persistent);

            m_JobBoundsDatas      = new NativeArray<DSplineBoundsData>(m_Group.Length, Allocator.Persistent);
            m_JobBoundsOutline    = new NativeArray<float>(m_Group.Length, Allocator.Persistent);
            m_JobDatas            = new NativeArray<DSplineData>(m_Group.Length, Allocator.Persistent);
            m_jobFormulaAddLength = new NativeArray<int>(m_Group.Length, Allocator.Persistent);
            m_refreshTypes        = new NativeArray<EActivationZone>(m_Group.Length, Allocator.Persistent);

            m_OrderedPoints = new TransformAccessArray(0);

            //m_EventArchetype = EntityManager.CreateArchetype(typeof(InitEvent));

            SendUpdateEvent(Entity.Null);
        }

        protected override void OnDestroyManager()
        {
            LastJobHandle.Complete();

            Camera.onPreCull -= OnCameraPreCull;
            m_FinalFillerArray.Dispose();
            m_JobBoundsDatas.Dispose();
            m_JobBoundsOutline.Dispose();
            m_JobDatas.Dispose();
            m_jobFormulaAddLength.Dispose();
            m_refreshTypes.Dispose();
            m_OrderedPoints.Dispose();
        }

        private void OnCameraPreCull(Camera cam)
        {
            var length = m_Group.Length;
            
            //< -------- -------- -------- -------- -------- -------- -------- ------- //
            // Finish the current job
            //> -------- -------- -------- -------- -------- -------- -------- ------- //
            var cameraBounds = new NativeArray<Bounds>(1, Allocator.TempJob);
            cameraBounds[0] = new Bounds(cam.transform.position, cam.GetExtents()).Flat2D();
            var resultsInterestBounds = new NativeArray<byte>(length, Allocator.TempJob);
            
            Profiler.BeginSample("Job");
            LastJobHandle.Complete();
            new JobCheckInterestects
            {
                BoundsDatas   = m_JobBoundsDatas,
                CameraBounds  = cameraBounds,
                BoundsOutline = m_JobBoundsOutline,
                Results       = resultsInterestBounds
            }.Run(length);
            Profiler.EndSample();

            var array = new Vector3[0];
            
            UpdateInjectedComponentGroups();

            Profiler.BeginSample("Loop");
            var currentFillerArrayLength = 0;
            for (var i = 0; i < length; i++)
            {
                var fillArrayAddLength = m_jobFormulaAddLength[i];
                if (m_refreshTypes[i] == EActivationZone.Bounds
                    && resultsInterestBounds[i] == 0)
                {
                    // ignore spline...
                    currentFillerArrayLength += fillArrayAddLength;
                    continue;
                }

                var data     = m_Group.SplineData[i];
                var renderer = m_Group.SplineRenderers[i];

                if (renderer.CameraRenderCount > 0)
                {
                    currentFillerArrayLength += fillArrayAddLength;
                    continue;
                }

                renderer.CameraRenderCount++;

                var maxFillerArrayIndexes = currentFillerArrayLength + fillArrayAddLength;
                var count                 = maxFillerArrayIndexes - currentFillerArrayLength;

                if (renderer.LastLineRendererPositionCount != count)
                {
                    renderer.LineRenderer.positionCount    = count;
                    renderer.LastLineRendererPositionCount = count;
                }

                // TODO: Get the array from a pool
                if (array.Length != count) array = new Vector3[count];

                SetArrayFromNative(array, m_FinalFillerArray, currentFillerArrayLength, count);

                renderer.LineRenderer.SetPositions(array);

                currentFillerArrayLength += fillArrayAddLength;
            }

            Profiler.EndSample();

            resultsInterestBounds.Dispose();
            cameraBounds.Dispose();
        }

        // based on this gist: https://gist.github.com/LotteMakesStuff/c2f9b764b15f74d14c00ceb4214356b4
        // modified to support start offset and custom size.
        private unsafe void SetArrayFromNative(Vector3[] managedBuffer, NativeArray<float3> unmanagedBuffer, int start, long size)
        {
            fixed (Vector3* vertexArrayPointer = managedBuffer)
            {
                var buffer = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(unmanagedBuffer);
                var ptr    = new IntPtr(buffer) + start * UnsafeUtility.SizeOf<float3>();

                UnsafeUtility.MemCpy // wow, this is so fast
                (
                    vertexArrayPointer,
                    ptr.ToPointer(),
                    size * UnsafeUtility.SizeOf<float3>()
                );
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //< -------- -------- -------- -------- -------- -------- -------- ------- //
            // Check for any changes
            //> -------- -------- -------- -------- -------- -------- -------- ------- //
            var hasChange = m_Events > 0;
            if (hasChange || Input.GetKeyDown(KeyCode.A))
            {
                m_OrderedPoints.Dispose();
                m_OrderedPoints = new TransformAccessArray(0);

                m_PointsLength = 0;
                for (int i = 0, total = 0, totalLength = 0;
                    i != m_Group.Length;
                    ++i)
                {
                    var renderer = m_Group.SplineRenderers[i];
                    var length   = renderer.Points.Length;
                    totalLength += length;

                    for (var j = 0;
                        j != length;
                        ++j, ++total)
                    {
                        if (m_OrderedPoints.length <= totalLength)
                            m_OrderedPoints.Add(renderer.Points[j]);
                        else
                            m_OrderedPoints[total] = renderer.Points[j];

                        ++m_PointsLength;
                    }
                }
            }

            m_Events = 0;

            if (m_Group.Length != m_JobBoundsDatas.Length)
            {
                m_JobBoundsDatas.Dispose();
                m_JobDatas.Dispose();
                m_JobBoundsOutline.Dispose();
                m_jobFormulaAddLength.Dispose();
                m_refreshTypes.Dispose();
                m_JobBoundsDatas      = new NativeArray<DSplineBoundsData>(m_Group.Length, Allocator.Persistent);
                m_JobDatas            = new NativeArray<DSplineData>(m_Group.Length, Allocator.Persistent);
                m_JobBoundsOutline    = new NativeArray<float>(m_Group.Length, Allocator.Persistent);
                m_jobFormulaAddLength = new NativeArray<int>(m_Group.Length, Allocator.Persistent);
                m_refreshTypes        = new NativeArray<EActivationZone>(m_Group.Length, Allocator.Persistent);
            }

            //< -------- -------- -------- -------- -------- -------- -------- ------- //
            // Create variables and jobs
            //> -------- -------- -------- -------- -------- -------- -------- ------- //
            // Create variables
            int fillerArrayLength = 0,
                currentCount      = 0,
                transformCount    = 0;
            // Create the job that will convert the UTransforms positions into the right components. 
            var localPointsToConvert = new NativeArray<float3>(m_PointsLength, Allocator.TempJob);
            var worldPointsToConvert = new NativeArray<float3>(m_PointsLength, Allocator.TempJob);
            var convertPointsJob = new JobConvertPoints
            {
                Result      = localPointsToConvert,
                WorldResult = worldPointsToConvert
            };
            // ...
            // Schedule the job
            inputDeps = convertPointsJob.Schedule(m_OrderedPoints, inputDeps);

            var pointsIndexes           = new NativeArray<int>(m_Group.Length, Allocator.TempJob);
            var finalFillerArrayIndexes = new NativeArray<int>(m_Group.Length, Allocator.TempJob);
            for (var i = 0; i != m_Group.Length; i++)
            {
                var data       = m_Group.SplineData[i];
                var boundsData = m_Group.SplineBoundsData[i];
                var renderer   = m_Group.SplineRenderers[i];

                renderer.CameraRenderCount = 0; //< Reset camera renders

                var fillArrayAddLength =
                    CGraphicalCatmullromSplineUtility.GetFormula(data.Step, renderer.Points.Length);

                pointsIndexes[i]           =  currentCount;
                finalFillerArrayIndexes[i] =  fillerArrayLength;
                currentCount               += renderer.Points.Length;
                fillerArrayLength          += fillArrayAddLength;

                m_JobDatas[i]            = data;
                m_JobBoundsDatas[i]      = boundsData;
                m_JobBoundsOutline[i]    = renderer.RefreshBoundsOutline;
                m_jobFormulaAddLength[i] = fillArrayAddLength;
                m_refreshTypes[i]        = renderer.RefreshType;
            }

            if (m_FinalFillerArray.Length != fillerArrayLength)
            {
                m_FinalFillerArray.Dispose();
                m_FinalFillerArray = new NativeArray<float3>(fillerArrayLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            var fillArrayJob = new JobFillArray();
            fillArrayJob.Datas                   = m_JobDatas;
            fillArrayJob.BoundsDatas             = m_JobBoundsDatas;
            fillArrayJob.Points                  = localPointsToConvert;
            fillArrayJob.WorldPoints             = worldPointsToConvert;
            fillArrayJob.PointsIndexes           = pointsIndexes;
            fillArrayJob.FinalFillerArray        = m_FinalFillerArray;
            fillArrayJob.FinalFillerArrayIndexes = finalFillerArrayIndexes;
            fillArrayJob.MaxLength               = fillerArrayLength;

            inputDeps = LastJobHandle = fillArrayJob.Schedule(inputDeps);

            return inputDeps;
        }

        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        // Groups
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        private struct Group
        {
            [ReadOnly] public ComponentDataArray<DSplineData>       SplineData;
            [ReadOnly] public ComponentDataArray<DSplineBoundsData> SplineBoundsData;

            [ReadOnly] public ComponentArray<SplineRendererBehaviour> SplineRenderers;
            // We only want valid spline
            public ComponentDataArray<DSplineValidTag> Valided;

            //[ReadOnly] public FixedArrayArray<DSplinePositionData>     Positions; // Useless?
            public          EntityArray Entities;
            public readonly int         Length;
        }

        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        // Jobs
        // -------- -------- -------- -------- -------- -------- -------- -------- -------- /.
        [BurstCompile]
        private struct JobConvertPoints : IJobParallelForTransform
        {
            [WriteOnly] public NativeArray<float3> Result;
            [WriteOnly] public NativeArray<float3> WorldResult;

            public void Execute(int index, TransformAccess transform)
            {
                Result[index]      = transform.localPosition;
                WorldResult[index] = transform.position;
            }
        }

        //[BurstCompile]
        private struct JobFillArray : IJob
        {
            [ReadOnly]                             public NativeArray<DSplineData>       Datas;
            [WriteOnly]                            public NativeArray<DSplineBoundsData> BoundsDatas;
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<float3>            WorldPoints;
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<float3>            Points;
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int>               PointsIndexes;
            [WriteOnly]                            public NativeArray<float3>            FinalFillerArray;

            [ReadOnly] [DeallocateOnJobCompletion]
            public NativeArray<int> FinalFillerArrayIndexes;

            public int MaxLength;

            // Unity jobs
            public void Execute()
            {
                for (var index = 0; index != Datas.Length; index++)
                {
                    var data       = Datas[index];
                    var boundsData = new DSplineBoundsData();

                    var currentPointsIndex      = PointsIndexes[index];
                    var currentFillerArrayIndex = FinalFillerArrayIndexes[index];
                    var sliceValue              = data.Step;
                    var tensionValue            = data.Tension;
                    var isLoopingValue          = data.IsLooping == 1;

                    var maxPointsIndexes = PointsIndexes.Length > index + 1 ? PointsIndexes[index + 1] : Points.Length;
                    var maxFillerArrayIndexes = FinalFillerArrayIndexes.Length > index + 1
                        ? FinalFillerArrayIndexes[index + 1]
                        : MaxLength;

                    CGraphicalCatmullromSplineUtility.CalculateCatmullromSpline(Points, currentPointsIndex,
                        maxPointsIndexes,
                        FinalFillerArray, currentFillerArrayIndex, maxFillerArrayIndexes,
                        sliceValue,
                        tensionValue,
                        isLoopingValue);

                    for (var pointIndex = currentPointsIndex; pointIndex != maxPointsIndexes; pointIndex++)
                    {
                        var point = WorldPoints[pointIndex];

                        if (pointIndex == currentPointsIndex)
                        {
                            boundsData.Min = point.xy;
                            boundsData.Max = point.xy;
                        }

                        var min = boundsData.Min;
                        var max = boundsData.Max;
                        boundsData.Min = math.min(point.xy, min);
                        boundsData.Max = math.max(point.xy, max);
                    }

                    BoundsDatas[index]                          = boundsData;
                    FinalFillerArray[currentFillerArrayIndex]   = Points[currentPointsIndex];
                    FinalFillerArray[maxFillerArrayIndexes - 1] = Points[maxPointsIndexes - 1];
                }
            }
        }

        [BurstCompile]
        private struct JobCheckInterestects : IJobParallelFor
        {
            [ReadOnly]  public NativeArray<DSplineBoundsData> BoundsDatas;
            [ReadOnly]  public NativeArray<Bounds>            CameraBounds;
            [ReadOnly]  public NativeArray<float>             BoundsOutline;
            [WriteOnly] public NativeArray<byte>              Results;

            public void Execute(int index)
            {
                var cb = CameraBounds[0];
                var bd = BoundsDatas[index];
                var bo = BoundsOutline[index];

                bd.Min -= bo;
                bd.Max += bo;

                var boolean = cb.min.x <= bd.Max.x && cb.max.x >= bd.Min.x
                                                   && cb.min.y <= bd.Max.y && cb.max.y >= bd.Min.y;

                Results[index] = (byte) (boolean ? 1 : 0);
            }
        }
    }
}