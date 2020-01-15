using Unity.Entities;
using UnityEngine;

namespace Patapon.Mixed.RhythmEngine.Flow
{
	public struct FlowPressure : IComponentData
	{
		public const float  Error   = 0.99f;
		public const double Perfect = 0.25f;

		/// <summary>
		///     Our custom Rhythm Key (Pata 1, Pon 2, Don 3, Chaka 4)
		/// </summary>
		public int KeyId;

		public int RenderBeat;

		/// <summary>
		///     The time of the beat (in ms tick), it will be used to do server side check
		/// </summary>
		public int Time;

		/// <summary>
		///     The score of the pressure [range -1 - 1, where 0 is perfect]
		/// </summary>
		/// <example>
		///     Let's say we made an engine with BeatInterval = 0.5f.
		///     The current time is 14.2f.
		///     The actual beat is timed at 14f.
		///     The score is 0.2f.
		///     If we made one at 13.8f, the score should be the same (but negative)!
		/// </example>
		public float Score;

		public FlowPressure(int keyId, int beatInterval, int timeMs)
		{
			var process = new FlowEngineProcess {Milliseconds = timeMs};
			RenderBeat = process.GetFlowBeat(beatInterval);

			Score = FlowEngineProcess.GetScore(timeMs, beatInterval);

			KeyId = keyId;
			Time  = timeMs;
		}

		public float GetAbsoluteScore()
		{
			return Mathf.Abs(Score);
		}
	}
}