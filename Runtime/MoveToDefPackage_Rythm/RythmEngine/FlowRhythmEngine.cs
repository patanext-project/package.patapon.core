using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace package.patapon.core
{
    public static class FlowRhythmEngine
    {
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
        /// <param name="interval">The interval between each beat</param>
        /// <returns></returns>
        public static float GetScore(int timeTick, int interval)
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
        public int TimeTick;
        public int StartTime;

        public double TimeReal => TimeTick * 0.001;

        /// <summary>
        /// Return the current beat from the time plus interval.
        /// </summary>
        /// <param name="beatInterval"></param>
        /// <returns></returns>
        public int GetActivationBeat(int beatInterval)
        {
            if (TimeTick == 0 || beatInterval == 0)
                return 0;

            var b = TimeTick / beatInterval;
            if (TimeTick < 0)
                b--;
            return b;
        }

        /// <summary>
        /// Return the current located beat inside time and interval range.
        /// Compared to <see cref="GetActivationBeat"/>, there is an offset of beatInterval/2
        /// </summary>
        /// <param name="beatInterval"></param>
        /// <returns></returns>
        public int GetFlowBeat(int beatInterval)
        {
            if (TimeTick == 0 || beatInterval == 0)
                return 0;

            var offsetTime = TimeTick + (beatInterval * Math.Sign(beatInterval) / 2);
            if (offsetTime == 0)
                return 0;

            var b = offsetTime / beatInterval;
            if (offsetTime < 0)
                b--;
            return b;
        }
        
        public static int CalculateActivationBeat(int timeTick, int interval)
        {
            return new RhythmEngineProcess {TimeTick = timeTick}.GetActivationBeat(interval);
        }

        public static int CalculateFlowBeat(int timeTick, int interval)
        {
            return new RhythmEngineProcess {TimeTick = timeTick}.GetFlowBeat(interval);
        }
    }

    public struct RhythmPressureData : IComponentData
    {
        public const float Error = 0.9f;
        
        /// <summary>
        /// Our custom Rhythm Key (Pata 1, Pon 2, Don 3, Chaka 4) 
        /// </summary>
        public int KeyId;

        public int RenderBeat;

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
            var process = new RhythmEngineProcess {TimeTick = timeTick};
            RenderBeat = process.GetFlowBeat(beatInterval);

            Score = FlowRhythmEngine.GetScore(timeTick, beatInterval);

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