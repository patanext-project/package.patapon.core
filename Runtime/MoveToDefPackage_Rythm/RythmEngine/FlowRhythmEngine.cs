using System;
using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace package.patapon.core
{
    public static class FlowRhythmEngine
    {
        /// <summary>
        /// Compute the score from a beat and time.
        /// </summary>
        /// <param name="tick">The tick</param>
        /// <param name="interval">The interval between each beat</param>
        /// <returns></returns>
        public static float GetScore(long timeMs, int interval)
        {
            var beatTimeDelta  = timeMs % interval;
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

    public struct RhythmEngineProcess : IReadWriteComponentSnapshot<RhythmEngineProcess>
    {
        public int Milliseconds;
        public int StartTime;

        /// <summary>
        /// Return the current beat from the time plus interval.
        /// </summary>
        /// <param name="beatInterval"></param>
        /// <returns></returns>
        public int GetActivationBeat(int beatInterval)
        {
            if (Milliseconds == 0 || beatInterval == 0)
                return 0;

            // removed support for negative beat...
            return (int) (Milliseconds / beatInterval);
        }

        /// <summary>
        /// Return the current located beat inside time and interval range.
        /// Compared to <see cref="GetActivationBeat"/>, there is an offset of beatInterval/2
        /// </summary>
        /// <param name="beatInterval"></param>
        /// <returns></returns>
        public int GetFlowBeat(int beatInterval)
        {
            if (Milliseconds == 0 || beatInterval == 0)
                return 0;

            var offsetTime = Milliseconds + (beatInterval * Math.Sign(beatInterval) / 2);
            if (offsetTime == 0)
                return 0;

            // removed support for negative beat...
            return (int) (offsetTime / beatInterval);
        }

        public static int CalculateActivationBeat(int ms, int interval)
        {
            return new RhythmEngineProcess {Milliseconds = ms}.GetActivationBeat(interval);
        }

        public static int CalculateFlowBeat(int ms, int interval)
        {
            return new RhythmEngineProcess {Milliseconds = ms}.GetFlowBeat(interval);
        }

        public void WriteTo(DataStreamWriter writer, ref RhythmEngineProcess baseline, DefaultSetup setup, SerializeClientData jobData)
        {
            writer.WritePackedIntDelta(StartTime, baseline.StartTime, jobData.NetworkCompressionModel);
        }

        public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RhythmEngineProcess baseline, DeserializeClientData jobData)
        {
            StartTime = reader.ReadPackedIntDelta(ref ctx, baseline.StartTime, jobData.NetworkCompressionModel);
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

        public RhythmPressureData(int keyId, int beatInterval, int timeMs)
        {
            var process = new RhythmEngineProcess {Milliseconds = timeMs};
            RenderBeat = process.GetFlowBeat(beatInterval);

            Score = FlowRhythmEngine.GetScore(timeMs, beatInterval);

            KeyId = keyId;
            Time = timeMs;
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