using System;
using package.patapon.def.Data;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace package.patapon.core
{
    // Currently a WIP, so there are a lot of tests and unit tests in this class
    // TODO: Make it inheriting a special class for managing rhythm engine
    public abstract class FlowRhythmEngine : JobComponentSystem
    {
        #region Constants

        public const int   KeyInvalid          = 0;
        public const int   KeyPata             = 1;
        public const int   KeyPon              = 2;
        public const int   KeyDon              = 3;
        public const int   KeyChaka            = 4;
        public const float DefaultBeatInterval = 0.5f;

        #endregion

        private EntityQuery m_EngineGroup;

        /*struct ProcessEngineJob : IJobProcessComponentDataWithEntity<ShardRhythmEngine, FlowRhythmEngineProcessData, FlowRhythmEngineSettingsData>
        {
            [ReadOnly]
            public float DeltaTime;

            [ReadOnly]
            public int FrameCount;

            [NativeDisableParallelForRestriction]
            public NativeList<FlowRhythmBeatEventProvider.Create> CreateBeatEventList;

            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            private void NonBurst_ThrowWarning(Entity entity)
            {
                Debug.LogWarning($"Engine '{entity}' had a FlowRhythmEngineSettingsData.BeatInterval of 0 (or less), this is not accepted.");
            }

            public void Execute(Entity entity, int index, [ReadOnly] ref ShardRhythmEngine engine, ref FlowRhythmEngineProcessData process, [ReadOnly] ref FlowRhythmEngineSettingsData settings)
            {
                process.Time      += DeltaTime;
                process.TimeDelta += DeltaTime;

                if (settings.BeatInterval <= 0.0001f)
                {
                    NonBurst_ThrowWarning(entity);
                    return;
                }

                while (process.TimeDelta >= settings.BeatInterval)
                {
                    process.TimeDelta -= settings.BeatInterval;

                    process.Beat += 1;

                    CreateBeatEventList.Add(new FlowRhythmBeatEventProvider.Create
                    {
                        Target     = entity,
                        FrameCount = FrameCount,
                        Beat       = process.Beat
                    });
                }

                process.TimeDelta = math.max(process.TimeDelta, 0f);
            }
        }*/

        private NativeList<FlowRhythmBeatEventProvider.Create> m_DelayedBeatEventList;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_DelayedBeatEventList = World.GetOrCreateSystem<FlowRhythmBeatEventProvider>().GetEntityDelayedList();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // Update engine data
            /*inputDeps = new ProcessEngineJob
            {
                DeltaTime           = GetSingleton<GameTimeComponent>().DeltaTime,
                CreateBeatEventList = m_DelayedBeatEventList
            }.Schedule(this, inputDeps);*/

            return inputDeps;
        }

        public static (int original, int correct) GetRhythmBeat(int pressureTimeTick, int beatIntervalTick)
        {
            var original = pressureTimeTick != 0 ? pressureTimeTick / beatIntervalTick : 0;
            var add      = pressureTimeTick + (beatIntervalTick / 2);
            var correct  = add != 0 ? add / beatIntervalTick : 0;

            return (original, correct);
        }

        /// <summary>
        /// Compute the score from a beat and time.
        /// </summary>
        /// <param name="timeTick">The time</param>
        /// <param name="originalBeat">The ORIGINAL beat</param>
        /// <param name="interval">The interval between each beat</param>
        /// <returns></returns>
        public static float GetScore(int timeTick, int originalBeat, int interval)
        {
            var beatTimeDelta  = timeTick % interval;
            var halvedInterval = interval * 0.5;
            var correctedTime  = (beatTimeDelta - halvedInterval);

            // this may happen if 'beatInterval' is 0
            if (double.IsNaN(correctedTime))
            {
                correctedTime = 0.0;
                if (interval == default)
                {
                    throw new InvalidOperationException($"{nameof(interval)} is set to 0, which is not allowed in FlowRhythmEngine.GetScore()");
                }
            }

            return (float) ((correctedTime + -Math.Sign(correctedTime) * halvedInterval) / halvedInterval);
        }
    }

    public struct RhythmEngineSimulateTag : IComponentData
    {

    }

    public struct RhythmEngineProcess : IComponentData
    {
        public int Beat;
        public int TimeTick;
        public int StartTime;

        public double TimeReal => TimeTick * 0.001;
    }

    public struct RhythmPressureData : IComponentData
    {
        /// <summary>
        /// Our custom Rhythm Key (Pata 1, Pon 2, Don 3, Chaka 4) 
        /// </summary>
        public int KeyId;

        /// <summary>
        /// The original beat of the pressure
        /// </summary>
        public int OriginalBeat;

        /// <summary>
        /// The modified beat of the pressure (as it's shifted)
        /// </summary>
        public int CorrectedBeat;

        /// <summary>
        /// The time of the beat (in ms tick), it will be used to do server side check
        /// </summary>
        public int Time;

        /// <summary>
        /// The score of the pressure [range -1 - 1, where 0 is perfect]
        /// </summary>
        /// <example>
        /// Let's say we made an engine with BeatInterval = 0.5f.
        /// The current time is 14.2f.
        /// The actual beat is timed at 14f.
        /// The score is 0.2f.
        /// 
        /// If we made one at 13.8f, the score should be the same (but negative)!
        /// </example>
        public float Score;

        public RhythmPressureData(int keyId, int beatInterval, int timeTick)
        {
            (OriginalBeat, CorrectedBeat) = FlowRhythmEngine.GetRhythmBeat(timeTick, beatInterval);

            Score = FlowRhythmEngine.GetScore(timeTick, OriginalBeat, beatInterval);

            KeyId = keyId;
            Time  = timeTick;
        }

        public float GetAbsoluteScore()
        {
            return Mathf.Abs(Score);
        }
    }

    public struct RhythmBeatData : IComponentData
    {
        public int Beat;

        public RhythmBeatData(int beat)
        {
            Beat = beat;
        }
    }
}