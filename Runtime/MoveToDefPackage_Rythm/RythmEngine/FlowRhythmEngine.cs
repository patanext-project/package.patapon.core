using System;
using package.patapon.def.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.patapon.core
{
    // Currently a WIP, so there are a lot of tests and unit tests in this class
    // TODO: Make it inheriting a special class for managing rhythm engine
    public class FlowRhythmEngine : ComponentSystem
    {
        #region Constants

        public const int KeyInvalid = 0;
        public const int KeyPata    = 1;
        public const int KeyPon     = 2;
        public const int KeyDon     = 3;
        public const int KeyChaka   = 4;
        public const float DefaultBeatInterval = 0.5f;

        #endregion

        private EntityQuery m_EngineGroup;
        private EntityArchetype m_EventArchetype;

        struct ProcessEngineJob : IJobProcessComponentDataWithEntity<ShardRhythmEngine, FlowRhythmEngineProcessData, FlowRhythmEngineSettingsData>
        {
            [ReadOnly]
            public float DeltaTime;

            [ReadOnly]
            public int FrameCount;

            [ReadOnly]
            public EntityArchetype EventArchetype;

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

                while (process.TimeDelta > settings.BeatInterval)
                {
                    process.TimeDelta -= settings.BeatInterval;

                    process.Beat += 1;

                    var eventEntity = EntityCommandBuffer.CreateEntity(index, EventArchetype);
                    EntityCommandBuffer.SetComponent(index, eventEntity, new RhythmShardEvent(FrameCount));
                    EntityCommandBuffer.SetComponent(index, eventEntity, new RhythmShardTarget(entity));
                    EntityCommandBuffer.SetComponent(index, eventEntity, new FlowRhythmBeatData(process.Beat));
                }

                process.TimeDelta = math.max(process.TimeDelta, 0f);
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EventArchetype = EntityManager.CreateArchetype
            (
                ComponentType.ReadWrite<FlowRhythmEngineTypeDefinition>(),
                ComponentType.ReadWrite<RhythmShardEvent>(),
                ComponentType.ReadWrite<RhythmShardTarget>(),
                ComponentType.ReadWrite<RhythmBeatData>(),
                ComponentType.ReadWrite<FlowRhythmBeatData>()
            );
        }

        protected override void OnUpdate()
        {
            var deltaTime = Time.deltaTime;

            // Update engine data
            var jobHandle = new ProcessEngineJob
            {
                FrameCount          = Time.frameCount,
                DeltaTime           = deltaTime,
                EventArchetype      = m_EventArchetype,
                EntityCommandBuffer = PostUpdateCommands.ToConcurrent()
            }.Schedule(this);

            jobHandle.Complete();
        }

        public Entity AddPressure(Entity shardEngine, int keyType, EntityCommandBuffer ecb)
        {
            var processData  = EntityManager.GetComponentData<FlowRhythmEngineProcessData>(shardEngine);
            var settingsData = EntityManager.GetComponentData<FlowRhythmEngineSettingsData>(shardEngine);

            var actualBeat   = processData.Beat;
            var actualTime   = processData.Time;
            var beatInterval = settingsData.BeatInterval;

            int correctedBeat;

            var score = GetScore(actualTime, actualBeat, beatInterval, out correctedBeat);

            //Debug.Log($"Beat|Corrected: {actualBeat}|{correctedBeat}, time: {actualTime}, score: {Mathf.Abs(score)}");

            Entity newEntity;
            if (ecb.IsCreated)
            {
                newEntity = ecb.CreateEntity();
                ecb.AddComponent(newEntity, new FlowRhythmEngineTypeDefinition());
                ecb.AddComponent(newEntity, new RhythmShardEvent(Time.frameCount));
                ecb.AddComponent(newEntity, new RhythmShardTarget(shardEngine));
                ecb.AddComponent(newEntity, new RhythmPressure());
                ecb.AddComponent(newEntity, new FlowRhythmPressureData(keyType, actualBeat, correctedBeat, score));

                return newEntity;
            }

            newEntity = EntityManager.CreateEntity();
            EntityManager.AddComponent(newEntity, typeof(FlowRhythmEngineTypeDefinition));
            EntityManager.AddComponentData(newEntity, new RhythmShardEvent(Time.frameCount));
            EntityManager.AddComponentData(newEntity, new RhythmShardTarget(shardEngine));
            EntityManager.AddComponent(newEntity, typeof(RhythmPressure));
            EntityManager.AddComponentData(newEntity, new FlowRhythmPressureData(keyType, actualBeat, correctedBeat, score));

            return newEntity;
        }

        // TODO: Make it as an abstract method
        /// <summary>
        /// Create a new rhythm engine.
        /// </summary>
        /// <returns>The new entity which contain engine data</returns>
        public Entity AddEngine()
        {
            var entity = EntityManager.CreateEntity
            (
                typeof(ShardRhythmEngine),
                typeof(FlowRhythmEngineTypeDefinition),
                typeof(FlowRhythmEngineProcessData),
                typeof(FlowRhythmEngineSettingsData)
            );

            EntityManager.SetComponentData(entity, new ShardRhythmEngine {EngineType = ComponentType.Create<FlowRhythmEngineTypeDefinition>()});
            EntityManager.SetComponentData(entity, new FlowRhythmEngineSettingsData(DefaultBeatInterval));

            return entity;
        }

        /// <summary>
        /// Compute the score from a beat and time.
        /// </summary>
        /// <param name="time">The time</param>
        /// <param name="beat">The beat</param>
        /// <param name="beatInterval">The interval between each beat</param>
        /// <param name="correctedBeat">The new corrected beat (as it can be shifted to the next one)</param>
        /// <returns></returns>
        public static float GetScore(double time, int beat, float beatInterval, out int correctedBeat)
        {
            var beatTimeDelta  = time % beatInterval;
            var halvedInterval = beatInterval * 0.5f;
            var correctedTime  = (beatTimeDelta - halvedInterval);

            correctedBeat = correctedTime >= 0 ? beat + 1 : beat;

            return (float) (correctedTime + -Math.Sign(correctedTime) * halvedInterval) / halvedInterval;
        }
    }

    // this is a header
    public struct FlowRhythmEngineTypeDefinition : IComponentData
    {
        
    }

    public struct FlowRhythmEngineProcessData : IComponentData
    {
        public int    Beat;
        public float  TimeDelta;
        public double Time;

        public FlowRhythmEngineProcessData(int beat, float timeDelta, float time)
        {
            Beat      = beat;
            TimeDelta = timeDelta;
            Time      = time;
        }
    }

    public struct FlowRhythmEngineSettingsData : IComponentData
    {
        public float BeatInterval;

        public FlowRhythmEngineSettingsData(float beatInterval)
        {
            BeatInterval = beatInterval;
        }
    }

    public struct FlowRhythmPressureData : IComponentData
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

        public FlowRhythmPressureData(int keyId, int originBeat, int correctedBeat, float score)
        {
            KeyId         = keyId;
            OriginalBeat  = originBeat;
            CorrectedBeat = correctedBeat;
            Score         = score;
        }

        public FlowRhythmPressureData(int keyId, FlowRhythmEngineSettingsData settingsData, FlowRhythmEngineProcessData processData)
        {
            Score = FlowRhythmEngine.GetScore(processData.Time, processData.Beat, settingsData.BeatInterval, out CorrectedBeat);

            KeyId        = keyId;
            OriginalBeat = processData.Beat;
        }

        public float GetAbsoluteScore()
        {
            return Mathf.Abs(Score);
        }
    }

    public struct FlowRhythmBeatData : IComponentData
    {
        public int Beat;

        public FlowRhythmBeatData(int beat)
        {
            Beat = beat;
        }
    }
}