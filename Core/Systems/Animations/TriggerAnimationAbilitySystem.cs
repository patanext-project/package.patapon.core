using System;
using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Base
{
	public abstract class TriggerAnimationAbilitySystem<TAbility> : TriggerAnimationAbilitySystem<TAbility, ValueTuple>
		where TAbility : struct, IComponentData
	{
		
	}
	
	public abstract class TriggerAnimationAbilitySystem<TAbility, TSystemDataSupplements> : BaseAbilityAnimationSystem, IAbilityPlayableSystemCalls
		where TAbility : struct, IComponentData
	{
		public struct SystemData
		{
			public TSystemDataSupplements Supplements;

			public int    Key;
			public double StartTime;
			public float  Weight;

			public Dictionary<string, AnimationClip> LoadedClips;
		}

		protected virtual string        AnimationPrefix      => $"{typeof(TAbility).Name}/Animations/";
		public virtual    EAbilityPhase KeepAnimationAtPhase => EAbilityPhase.ActiveOrChaining;

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

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim      = animation.CurrAnimation;
			var abilityEntity = AbilityFinder.GetAbility(backend.DstEntity);
			EntityManager.TryGetComponentData<AbilityState>(abilityEntity, out var abilityState);
			
			if (abilityEntity != default)
				InjectAnimationWithSystemData<SystemData>();
			
			if (abilityEntity == default || (abilityState.Phase & KeepAnimationAtPhase) == 0)
			{
				if (currAnim.Type == SystemType && ((abilityState.Phase & EAbilityPhase.Chaining) == 0 || animation.RootTime > currAnim.StopAt))
				{
					Console.WriteLine("remove anim");
					animation.SetTargetAnimation(new TargetAnimation(default, previousType: currAnim.Type));
				}

				return;
			}
			
			OnUpdate(targetEntity, backend, animation, abilityEntity, abilityState);
		}

		protected abstract void OnUpdate(Entity targetEntity,  UnitVisualBackend backend, UnitVisualAnimation animation,
		                                 Entity abilityEntity, AbilityState      abilityState);

		protected override IAbilityPlayableSystemCalls GetPlayableCalls()
		{
			return this;
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(TAbility), typeof(AbilityState), typeof(Owner));
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
					var cp = AnimationClipPlayable.Create(behavior.Graph, (AnimationClip) handle.Result);
					if (cp.IsNull())
						throw new InvalidOperationException("null clip");

					behavior.Graph.Connect(cp, 0, behavior.Mixer, index);

					behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType)
					        .LoadedClips[m_Triggers[index]] = (AnimationClip) handle.Result;
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