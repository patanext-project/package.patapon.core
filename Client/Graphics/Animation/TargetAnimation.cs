using System;
using Unity.Mathematics;

namespace package.patapon.core.Animation
{
	public struct TargetAnimation
	{
		public static readonly TargetAnimation Null = new TargetAnimation(null);

		/// <summary>
		/// The system type of the animation
		/// </summary>
		public readonly Type Type;

		/// <summary>
		/// The previous system type before the new animation (used for transition)
		/// </summary>
		public readonly Type PreviousType;

		/// <summary>
		/// Can other animations override this one?
		/// (There can be some exceptions where some animations can override on some conditions or if it's urgent)
		/// </summary>
		public readonly bool AllowOverride;

		public readonly bool AllowTransition;

		/// <summary>
		/// The current weight of this animation, use this for transition
		/// </summary>
		public readonly float Weight;

		public readonly double TransitionStart;
		public readonly double TransitionEnd;
		public readonly double StopAt;

		public TargetAnimation(Type type, bool allowOverride = true, bool allowTransition = true, float weight = 0, double transitionStart = -1, double transitionEnd = -1, double stopAt = -1, Type previousType = null)
		{
			Type            = type;
			PreviousType    = previousType;
			AllowOverride   = allowOverride;
			AllowTransition = allowTransition;
			Weight          = weight;
			TransitionStart = transitionStart;
			TransitionEnd   = transitionEnd;
			StopAt          = stopAt;
		}

		public static bool operator ==(TargetAnimation left, TargetAnimation right)
		{
			return left.Type == right.Type;
		}

		public static bool operator !=(TargetAnimation left, TargetAnimation right)
		{
			return left.Type != right.Type;
		}

		public float GetTransitionWeightFixed(double time, float fxd = 1)
		{
			if (TransitionStart < 0 || TransitionEnd < 0)
				return 0;
			if (time > TransitionEnd)
				return 0;
			if (time < TransitionStart)
				return fxd;
			return (float) (1 - math.unlerp(TransitionStart, TransitionEnd, time));
		}

		public bool CanStartAnimationAt(double time)
		{
			if (AllowTransition && TransitionStart >= 0 && time >= TransitionEnd)
				return true;
			return StopAt < 0 || time >= StopAt;
		}

		public bool CanBlend(double time)
		{
			return AllowTransition && TransitionStart >= 0 && time <= TransitionEnd;
		}
	}
}