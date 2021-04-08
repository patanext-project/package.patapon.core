using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Karambolo.Common;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Base
{
	public abstract class TriggerAnimationSystem : TriggerAnimationSystem<ValueTuple>
	{
		
	}
	
	public abstract class TriggerAnimationSystem<TSystemDataSupplements> : BaseAnimationSystem, IAbilityPlayableSystemCalls
	{
		public struct SystemData
		{
			public TSystemDataSupplements Supplements;

			public int    Key;
			public double StartTime;
			public float  Weight;

			public Dictionary<string, AnimationClip> LoadedClips;
		}

		public abstract string FolderName { get; }

		protected virtual string AnimationPrefix => $"{FolderName}/Animations/";
		
		public virtual bool AllowOverride => true;

		protected abstract string[] GetTriggers();

		private string[] m_Triggers;

		protected override void OnCreate()
		{
			m_Triggers = GetTriggers();
			
			base.OnCreate();
		}

		protected override AnimationMap GetAnimationMap()
		{
			var animationMap = new AnimationMap<int>(AnimationPrefix);
			for (var i = 0; i < m_Triggers.Length; i++)
			{
				animationMap.Add(m_Triggers[i], i++);
			}

			return animationMap;
		}

		public bool KeepOrTrigger(string key, float transitionStart = -1f, float transitionEnd = -1f, float? stopAt = null)
		{
			ref var systemData = ref CurrentVisualAnimation.GetSystemData<SystemData>(SystemType);
			if (systemData.Key != Array.IndexOf(m_Triggers, key))
			{
				Trigger(key, transitionStart, transitionEnd, stopAt ?? -1);
				return true;
			}

			ResetIdleTime(CurrentVisualAnimation.Backend.DstEntity);

			CurrentVisualAnimation.SetTargetAnimation(new TargetAnimation(SystemType, allowOverride: AllowOverride,
				transitionStart: transitionStart, transitionEnd: transitionEnd,
				stopAt: stopAt ?? CurrentVisualAnimation.RootTime + 0.25f));

			return false;
		}

		public void Trigger(string key, double transitionStart = -1f, double transitionEnd = -1f, double stopAt = -1)
		{
			ref var systemData = ref CurrentVisualAnimation.GetSystemData<SystemData>(SystemType);
			systemData.StartTime = CurrentVisualAnimation.RootTime;
			systemData.Key       = Array.IndexOf(m_Triggers, key);

			ResetIdleTime(CurrentVisualAnimation.Backend.DstEntity);

			CurrentVisualAnimation.SetTargetAnimation(new TargetAnimation(SystemType, allowOverride: AllowOverride,
				transitionStart: transitionStart, transitionEnd: transitionEnd,
				stopAt: stopAt));
		}

		public void Stop(float transition = 0f, float stopAt = 0f)
		{
			ref var systemData = ref CurrentVisualAnimation.GetSystemData<SystemData>(SystemType);
			systemData.Weight = 0;

			if (CurrentVisualAnimation.CurrAnimation.Type == SystemType)
			{
				CurrentVisualAnimation.SetTargetAnimation(new TargetAnimation(SystemType,
					transitionStart: CurrentVisualAnimation.RootTime, transitionEnd: CurrentVisualAnimation.RootTime + transition,
					stopAt: CurrentVisualAnimation.RootTime + stopAt));
			}
		}

		protected abstract bool IsValidTarget(Entity       targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation);
		protected abstract bool ShouldStopAnimation(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation);
		
		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			if (!IsValidTarget(targetEntity, backend, animation))
				return;
			
			InjectAnimationWithSystemData<SystemData>();

			var currAnim = animation.CurrAnimation;
			if (ShouldStopAnimation(targetEntity, backend, animation))
			{
				if (currAnim.Type == SystemType)
					animation.SetTargetAnimation(new TargetAnimation(default, previousType: currAnim.Type));
				return;
			}

			OnUpdateForTriggers(targetEntity, backend, animation);
		}

		protected abstract void OnUpdateForTriggers(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation);

		protected override IAbilityPlayableSystemCalls GetPlayableCalls()
		{
			return this;
		}

		void IAbilityPlayableSystemCalls.OnInitialize(PlayableInnerCall behavior)
		{
			ref var systemData = ref behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);
			systemData.LoadedClips = new Dictionary<string, AnimationClip>();
			
			behavior.Mixer.SetInputCount(m_Triggers.Length);
			for (var i = 0; i < m_Triggers.Length; i++)
			{
				var index = i;
				behavior.AddAsyncOp(AnimationMap.Resolve(m_Triggers[i], GetCurrentClipProvider()), handle =>
				{
					var cp = AnimationClipPlayable.Create(behavior.Graph, ((Task<AnimationClip>) handle).Result);
					if (cp.IsNull())
						throw new InvalidOperationException("null clip");

					behavior.Graph.Connect(cp, 0, behavior.Mixer, index);

					behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType)
					        .LoadedClips[m_Triggers[index]] = ((Task<AnimationClip>) handle).Result;
				});
			}
		}

		void IAbilityPlayableSystemCalls.PrepareFrame(PlayableInnerCall behavior, Playable playable, FrameData info)
		{
			ref var systemData = ref behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);

			var global   = (float) behavior.Root.GetTime() - systemData.StartTime;
			var currAnim = behavior.Visual.CurrAnimation;

			behavior.Mixer.SetTime(global);

			var inputCount = behavior.Mixer.GetInputCount();
			for (var i = 0; i != inputCount; i++) behavior.Mixer.SetInputWeight(i, i == systemData.Key ? 1 : 0);

			systemData.Weight = 0;
			if (currAnim.CanBlend(behavior.Root.GetTime()) && (currAnim.PreviousType == SystemType && currAnim.Type != SystemType))
				systemData.Weight = currAnim.GetTransitionWeightFixed(behavior.Root.GetTime());
			else if (currAnim.Type == SystemType)
				systemData.Weight = 1;
			
			behavior.Root.SetInputWeight(VisualAnimation.GetIndexFrom(behavior.Root, behavior.Self), systemData.Weight);
		}
	}
}