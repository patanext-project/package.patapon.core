using System;
using PataNext.Client.Graphics.Animation.Base;
using UnityEngine;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units.Base
{
	public struct KeyedHandleData<T> : IAbilityAnimationKey
	{
		public T      Value;
		public string Key { get; set; }
	}

	public abstract class BaseCompleteAbilityAnimationSystem<THandleData, TClip, TSystemData> : BaseAbilityAnimationSystem
	<
		BaseCompleteAbilityAnimationSystem<THandleData, TClip, TSystemData>.PlayableSystem,
		BaseCompleteAbilityAnimationSystem<THandleData, TClip, TSystemData>.Init,
		TSystemData,
		KeyedHandleData<THandleData>,
		TClip
	>
		where TSystemData : struct, IPlayableSystemData<BaseCompleteAbilityAnimationSystem<THandleData, TClip, TSystemData>.PlayableSystem>
		where THandleData : struct
		where TClip : struct, IAbilityAnimClip
	{
		public struct Init
		{
			public BaseCompleteAbilityAnimationSystem<THandleData, TClip, TSystemData> System;
		}

		public class PlayableSystem : BaseAbilityPlayable<Init>
		{
			public BaseCompleteAbilityAnimationSystem<THandleData, TClip, TSystemData> System;

			protected override void OnInitialize(Init init)
			{
				if (GetType() == typeof(PlayableSystem))
					throw new InvalidOperationException("Replace GetNewPlayable");
				
				System = init.System;
				Debug.Log("OnInitialize SystemType: " + SystemType);
				System.PlayableOnInitialize(this, ref Visual.VisualAnimation.GetSystemData<TSystemData>(SystemType));
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				System.PlayablePrepareFrame(this, playable, info, ref Visual.VisualAnimation.GetSystemData<TSystemData>(SystemType));
			}
		}

		protected virtual void PlayableOnInitialize(PlayableSystem behavior, ref TSystemData systemData)
		{

		}

		protected virtual void PlayablePrepareFrame(PlayableSystem behavior, Playable playable, FrameData info, ref TSystemData systemData)
		{

		}

		protected override void OnAnimationInject(UnitVisualAnimation animation, ref Init initData)
		{
		}

		protected void InjectAnimation(UnitVisualAnimation animation)
		{
			InjectAnimation(animation, new Init {System = this});
		}

		public void PreLoadAnimationAsset(string path, string key, THandleData handle)
		{
			PreLoadAnimationAsset(path, new KeyedHandleData<THandleData> {Key = key, Value = handle});
		}
	}
}