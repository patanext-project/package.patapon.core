using System;
using package.patapon.def;
using package.patapon.def.Data;
using package.stormiumteam.shared;
using Scripts;
using Unity.Entities;
using UnityEngine;

namespace package.patapon.core
{
    // Currently a WIP, so there are a lot of tests and unit tests in this class
    // TODO: Make it inheriting a special class for managing rhythm engine
    [UpdateInGroup(typeof(PlayUpdateOrder.RhythmEngineOrder))]
    public class SequenceRhythmEngine : ComponentSystem
    {
        struct EngineGroups
        {
            public ComponentDataArray<ShardRhythmEngine>                ShardArray;
            public ComponentDataArray<SequenceRhythmEngineProcessData>  ProcessArray;
            public ComponentDataArray<SequenceRhythmEngineSettingsData> SettingsArray;
            public EntityArray                                          Entities;

            public readonly int Length;
        }

        #region Constants

        public const int KeyInvalid = 0;
        public const int KeyPata    = 1;
        public const int KeyPon     = 2;
        public const int KeyDon     = 3;
        public const int KeyChaka   = 4;
        public const float DefaultBeatInterval = 0.5f;

        #endregion

        #region Injections

        [Inject] private EndFrameBarrier m_EndFrameBarrier;
        [Inject] private EngineGroups m_EngineGroups;

        #endregion

        protected override void OnUpdate()
        {
            var deltaTime = Time.deltaTime;

            // Update engine data
            for (var i = 0; i != m_EngineGroups.Length; i++)
            {
                var entity       = m_EngineGroups.Entities[i];
                var processData  = m_EngineGroups.ProcessArray[i];
                var settingsData = m_EngineGroups.SettingsArray[i];

                processData.Time      += deltaTime;
                processData.TimeDelta += deltaTime;

                while (processData.TimeDelta > settingsData.BeatInterval)
                {
                    processData.TimeDelta -= settingsData.BeatInterval;

                    processData.Beat += 1;

                    PostUpdateCommands.CreateEntity();
                    PostUpdateCommands.AddComponent(new SequenceRythmEngineTypeDefinition());
                    PostUpdateCommands.AddComponent(new RhythmShardEvent(Time.frameCount));
                    PostUpdateCommands.AddComponent(new RhythmShardTarget(entity));
                    PostUpdateCommands.AddComponent(new RhythmBeatData());
                    PostUpdateCommands.AddComponent(new SequenceRhythmBeatData(processData.Beat));

                    // TODO: Find a way to make a direct event, it should be done after the iteration
                }

                processData.TimeDelta = Mathf.Max(processData.TimeDelta, 0f);

                m_EngineGroups.ProcessArray[i] = processData;
            }
        }

        public void AddPressure(Entity shardEngine, int keyType, EntityCommandBuffer ecf)
        {
            var processData = EntityManager.GetComponentData<SequenceRhythmEngineProcessData>(shardEngine);
            var settingsData = EntityManager.GetComponentData<SequenceRhythmEngineSettingsData>(shardEngine);

            var actualBeat   = processData.Beat;
            var actualTime   = processData.Time;
            var beatInterval = settingsData.BeatInterval;

            int correctedBeat;
            
            var score = GetScore(actualTime, actualBeat, beatInterval, out correctedBeat);

            Debug.Log($"Beat|Corrected: {actualBeat}|{correctedBeat}, time: {actualTime}, score: {Mathf.Abs(score)}");
            
            ecf.CreateEntity();
            ecf.AddComponent(new SequenceRythmEngineTypeDefinition());
            ecf.AddComponent(new RhythmShardEvent(Time.frameCount));
            ecf.AddComponent(new RhythmShardTarget(shardEngine));
            ecf.AddComponent(new RhythmPressure());
            ecf.AddComponent(new SequenceRhythmPressureData(keyType, actualBeat, correctedBeat, score));
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
                typeof(SequenceRythmEngineTypeDefinition),
                typeof(SequenceRhythmEngineProcessData),
                typeof(SequenceRhythmEngineSettingsData)
            );

            EntityManager.SetComponentData(entity, new ShardRhythmEngine {EngineType = ComponentType.Create<SequenceRythmEngineTypeDefinition>()});
            EntityManager.SetComponentData(entity, new SequenceRhythmEngineSettingsData(DefaultBeatInterval));

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
        public float GetScore(double time, int beat, float beatInterval, out int correctedBeat)
        {
            var beatTimeDelta = time % beatInterval;
            var halvedInterval = beatInterval * 0.5f;
            var correctedTime = (beatTimeDelta - halvedInterval);

            correctedBeat = correctedTime >= 0 ? beat + 1 : beat;

            return (float) (correctedTime + -Math.Sign(correctedTime) * halvedInterval);
        }
    }

    // this is a header
    public struct SequenceRythmEngineTypeDefinition : IComponentData
    {
        
    }

    public struct SequenceRhythmEngineProcessData : IComponentData
    {
        public int    Beat;
        public float  TimeDelta;
        public double Time;

        public SequenceRhythmEngineProcessData(int beat, float timeDelta, float time)
        {
            Beat      = beat;
            TimeDelta = timeDelta;
            Time      = time;
        }
    }

    public struct SequenceRhythmEngineSettingsData : IComponentData
    {
        public float BeatInterval;

        public SequenceRhythmEngineSettingsData(float beatInterval)
        {
            BeatInterval = beatInterval;
        }
    }

    public struct SequenceRhythmPressureData : IComponentData
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

        public SequenceRhythmPressureData(int keyId, int originBeat, int correctedBeat, float score)
        {
            KeyId         = keyId;
            OriginalBeat  = originBeat;
            CorrectedBeat = correctedBeat;
            Score         = score;
        }

        public float GetAbsoluteScore()
        {
            return Mathf.Abs(Score);
        }
    }

    public struct SequenceRhythmBeatData : IComponentData
    {
        public int Beat;

        public SequenceRhythmBeatData(int beat)
        {
            Beat = beat;
        }
    }
}