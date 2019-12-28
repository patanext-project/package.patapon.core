using System;
using System.Diagnostics.Contracts;
using Revolution;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon.Mixed.RhythmEngine.Flow
{
    public struct FlowSimulateProcess : IComponentData
    {

    }

    public struct FlowEngineProcess : IReadWriteComponentSnapshot<FlowEngineProcess>, ISnapshotDelta<FlowEngineProcess>
    {
        public int Milliseconds;
        public int StartTime;

        /// <summary>
        /// Return the current beat from the time plus interval.
        /// </summary>
        /// <param name="beatInterval"></param>
        /// <returns></returns>
        [Pure]
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
        [Pure]
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

        public void WriteTo(DataStreamWriter writer, ref FlowEngineProcess baseline, DefaultSetup setup, SerializeClientData jobData)
        {
            writer.WritePackedIntDelta(StartTime, baseline.StartTime, jobData.NetworkCompressionModel);
        }

        public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref FlowEngineProcess baseline, DeserializeClientData jobData)
        {
            this = baseline;
            StartTime = reader.ReadPackedIntDelta(ref ctx, baseline.StartTime, jobData.NetworkCompressionModel);
        }

        public bool DidChange(FlowEngineProcess baseline)
        {
            return StartTime != baseline.StartTime;
        }

        public struct Exclude : IComponentData
        {
        }

        public class NetSynchronize : MixedComponentSnapshotSystemDelta<FlowEngineProcess>
        {
            public override ComponentType ExcludeComponent => typeof(Exclude);
        }

        // ------------------------------------------------------------------ //
        // -------- STATIC -------
        // ------------------------------------------------------------------ //

        public static int CalculateActivationBeat(int ms, int interval)
        {
            return new FlowEngineProcess {Milliseconds = ms}.GetActivationBeat(interval);
        }

        public static int CalculateFlowBeat(int ms, int interval)
        {
            return new FlowEngineProcess {Milliseconds = ms}.GetFlowBeat(interval);
        }

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
}