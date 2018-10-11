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
    public class P4RhythmEngine : ComponentSystem
    {
        struct EngineGroups
        {
            public          ComponentDataArray<ShardRhythmEngine>         ShardArray;
            public          ComponentDataArray<P4ShardEngineProcessData>  ProcessArray;
            public          ComponentDataArray<P4ShardEngineSettingsData> SettingsArray;
            public readonly int                                           Length;
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

        [Inject] private RhythmFlowManager m_RhythmFlowMgr;
        [Inject] private EngineGroups m_EngineGroups;

        #endregion

        protected override void OnUpdate()
        {
            var deltaTime = Time.deltaTime;

            #region Decomment and move this to an unit test

            /*// Pata
            if (Input.GetKeyDown(KeyCode.Keypad4))
            {
                DoPressure(KeyPata);
            }
            // Pon
            else if (Input.GetKeyDown(KeyCode.Keypad6))
            {
                DoPressure(KeyPon);
            }
            // Don
            else if (Input.GetKeyDown(KeyCode.Keypad2))
            {
                DoPressure(KeyDon);
            }
            // Chaka
            else if (Input.GetKeyDown(KeyCode.Keypad8))
            {
                DoPressure(KeyChaka);
            }*/

            #endregion

            // Update engine data
            for (var i = 0; i != m_EngineGroups.Length; i++)
            {
                var processData  = m_EngineGroups.ProcessArray[i];
                var settingsData = m_EngineGroups.SettingsArray[i];

                processData.Time      += deltaTime;
                processData.TimeDelta += deltaTime;

                while (processData.TimeDelta > settingsData.BeatInterval)
                {
                    processData.TimeDelta -= settingsData.BeatInterval;

                    processData.Beat += 1;

                    // TODO: Find a better way to make an event, it should be done after the iteration
                    /*foreach (var manager in AppEvent<EventRhythmFlowNewBeat.IEv>.eventList)
                    {
                        AppEvent<EventRhythmFlowNewBeat.IEv>.Caller = this;
                        manager.Callback(new EventRhythmFlowNewBeat.Arguments(engine, pressureEntity));
                    }*/
                }

                processData.TimeDelta = Mathf.Max(processData.TimeDelta, 0f);

                m_EngineGroups.ProcessArray[i] = processData;
            }
        }

        public void AddPressure(Entity shardEngine, int keyType)
        {
            var processData = EntityManager.GetComponentData<P4ShardEngineProcessData>(shardEngine);
            var settingsData = EntityManager.GetComponentData<P4ShardEngineSettingsData>(shardEngine);

            var actualBeat   = processData.Beat;
            var actualTime   = processData.Time;
            var beatInterval = settingsData.BeatInterval;

            int correctedBeat;
            
            var score = GetScore(actualTime, actualBeat, beatInterval, out correctedBeat);

            Debug.Log($"Beat|Corrected: {actualBeat}|{correctedBeat}, time: {actualTime}, score: {Mathf.Abs(score)}");
            
            PostUpdateCommands.CreateEntity();
            PostUpdateCommands.AddComponent(new RhythmPressure());
            PostUpdateCommands.AddComponent(new P4PressureData(keyType, actualBeat, correctedBeat, score));
            PostUpdateCommands.AddComponent(new RhythmShardTarget(shardEngine));
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
                typeof(P4ShardEngineProcessData),
                typeof(P4ShardEngineSettingsData)
            );

            EntityManager.SetComponentData(entity, new ShardRhythmEngine {EngineType = ComponentType.Create<P4RythmEngineTypeDefinition>()});
            EntityManager.SetComponentData(entity, new P4ShardEngineSettingsData(DefaultBeatInterval));

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

    public struct P4RythmEngineTypeDefinition : IComponentData
    {
        
    }

    public struct P4ShardEngineProcessData : IComponentData
    {
        public int    Beat;
        public float  TimeDelta;
        public double Time;

        public P4ShardEngineProcessData(int beat, float timeDelta, float time)
        {
            Beat      = beat;
            TimeDelta = timeDelta;
            Time      = time;
        }
    }

    public struct P4ShardEngineSettingsData : IComponentData
    {
        public float BeatInterval;

        public P4ShardEngineSettingsData(float beatInterval)
        {
            BeatInterval = beatInterval;
        }
    }

    public struct P4PressureData : IComponentData
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

        public P4PressureData(int keyId, int originBeat, int correctedBeat, float score)
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
}