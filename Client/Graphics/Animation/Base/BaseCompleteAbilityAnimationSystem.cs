using package.patapon.core.Animation;
using package.patapon.core.Animation.Units;
using Patapon.Client.Graphics.Animation.Units;
using UnityEngine;
using UnityEngine.Playables;

namespace Graphics.Animation.Base
{
	public abstract class BaseCompleteAbilityAnimationSystem<TThis, TSystemData> : BaseAbilityAnimationSystem
	<
		BaseCompleteAbilityAnimationSystem<TThis, TSystemData>.PlayableSystem,
		BaseCompleteAbilityAnimationSystem<TThis, TSystemData>.Init,
		TSystemData
	>
		where TThis : BaseCompleteAbilityAnimationSystem<TThis, TSystemData>
		where TSystemData : struct, IPlayableSystemData<BaseCompleteAbilityAnimationSystem<TThis, TSystemData>.PlayableSystem>
	{
		public struct Init
		{
			public TThis System;
		}

		public class PlayableSystem : BaseAbilityPlayable<Init>
		{
			public TThis System;

			protected override void OnInitialize(Init init)
			{
				System = init.System;
				Debug.Log("OnInitialize SystemType: " + SystemType);
				System.PlayableOnInitialize(this, ref Visual.VisualAnimation.GetSystemData<TSystemData>(SystemType));
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				Debug.Log("prepare frame");
				System.PlayablePrepareFrame(this, playable, info, ref Visual.VisualAnimation.GetSystemData<TSystemData>(SystemType));
			}
		}

		protected override void OnAnimationAdded(ref VisualAnimation.ManageData data, ref TSystemData systemData)
		{
			Debug.Log("OnAnimationAdded SystemType: " + SystemType);
			SetInit(new Init {System = (TThis) this});
			base.OnAnimationAdded(ref data, ref systemData);
		}

		protected virtual void PlayableOnInitialize(PlayableSystem behavior, ref TSystemData systemData)
		{

		}

		protected virtual void PlayablePrepareFrame(PlayableSystem behavior, Playable playable, FrameData info, ref TSystemData systemData)
		{

		}
	}
}