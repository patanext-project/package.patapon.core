using System;
using package.patapon.def;
using package.patapon.def.Data;
using Unity.Entities;
using UnityEngine;

namespace package.patapon.core
{
    // Currently a WIP, so there are a lot of tests and unit tests in this class
    [AlwaysUpdateSystem]
    public class P4RhythmEngine : ComponentSystem
    {
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

        #endregion

        #region Public Fields

        public Entity ShardTarget;

        #endregion

        protected override void OnCreateManager()
        {
            ShardTarget = EntityManager.CreateEntity
            (
                typeof(ShardRhythmEngine),
                typeof(P4ShardEngineProcessData),
                typeof(P4ShardEngineSettingsData)
            );

            EntityManager.SetComponentData(ShardTarget, new ShardRhythmEngine {EngineType = ComponentType.Create<P4RythmEngineTypeDefinition>()});
            
            SetSettingsData(new P4ShardEngineSettingsData(DefaultBeatInterval));
        }

        protected override void OnUpdate()
        {
            var deltaTime = Time.deltaTime;
            
            // Pata
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
            }
            
            // Update engine data
            var settingsData = GetSettingsData();
            var processData = GetProcessData();

            processData.Time += deltaTime;
            processData.TimeDelta += deltaTime;

            while (processData.TimeDelta > settingsData.BeatInterval)
            {
                processData.TimeDelta -= settingsData.BeatInterval;

                processData.Beat += 1;
                
                Debug.Log($"Beat: {processData.Beat}; Time: {processData.Time:F2}; Left: {processData.TimeDelta:F2}");
                
                // todo: trigger events
            }

            processData.TimeDelta = Mathf.Max(processData.TimeDelta, 0f);

            SetProcessData(processData);
        }

        private void DoPressure(int keyType)
        {
            var processData  = GetProcessData();
            var settingsData = GetSettingsData();

            var actualBeat   = processData.Beat;
            var actualTime   = processData.Time;
            var beatInterval = settingsData.BeatInterval;

            int correctedBeat;
            
            var score = GetScore(actualTime, actualBeat, beatInterval, out correctedBeat);

            Debug.Log($"Beat|Corrected: {actualBeat}|{correctedBeat}, time: {actualTime}, score: {Mathf.Abs(score)}");
            
            PostUpdateCommands.CreateEntity();
            PostUpdateCommands.AddComponent(new RhythmPressure());
            PostUpdateCommands.AddComponent(new P4PressureData(keyType, actualBeat, correctedBeat, score));
            PostUpdateCommands.AddComponent(new RhythmShardTarget(ShardTarget));
        }

        public P4ShardEngineProcessData GetProcessData()
        {
            return EntityManager.GetComponentData<P4ShardEngineProcessData>(ShardTarget);
        }
        
        public P4ShardEngineSettingsData GetSettingsData()
        {
            return EntityManager.GetComponentData<P4ShardEngineSettingsData>(ShardTarget);
        }

        public void SetProcessData(P4ShardEngineProcessData data)
        {
            EntityManager.SetComponentData(ShardTarget, data);
        }
        
        public void SetSettingsData(P4ShardEngineSettingsData data)
        {
            EntityManager.SetComponentData(ShardTarget, data);
        }

        public float GetScore(double time, int beat, float beatInterval, out int correctedBeat)
        {
            correctedBeat = beat;

            var beatTimeDelta = time % beatInterval;
            var halvedInterval = beatInterval * 0.5f;

            var correctedTime = (beatTimeDelta - beatInterval * 0.5);
            if (correctedTime >= 0)
                correctedBeat = beat + 1;

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