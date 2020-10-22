using System;
using System.Collections.Generic;
using System.Linq;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units.CTate
{
	public abstract class SingleAnimationSystemBase : BaseAbilityAnimationSystem, IAbilityPlayableSystemCalls
	{
		public abstract string                      DefaultResourceClip { get; }
		public abstract string                      DefaultKeyClip      { get; }

		public virtual bool          AllowOverride        => true;
		public     virtual    EAbilityPhase KeepAnimationAtPhase => EAbilityPhase.Active;


		protected override IAbilityPlayableSystemCalls GetPlayableCalls()
		{
			return this;
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim      = animation.CurrAnimation;
			var abilityEntity = AbilityFinder.GetAbility(backend.DstEntity);
			EntityManager.TryGetComponentData<AbilityState>(abilityEntity, out var abilityState);
			if (abilityEntity == default || (abilityState.Phase & KeepAnimationAtPhase) == 0)
			{
				if (currAnim.Type == SystemType && ((abilityState.Phase & EAbilityPhase.Chaining) == 0 || animation.RootTime > currAnim.StopAt))
					animation.SetTargetAnimation(new TargetAnimation(default, previousType: currAnim.Type));

				return;
			}
			
			ResetIdleTime(targetEntity);

			if (!EntityManager.TryGetComponentData(EntityManager.GetComponentData<Owner>(abilityEntity).Target, out Relative<RhythmEngineDescription> engineRelative))
				return;

			var commandState   = EntityManager.GetComponentData<GameCommandState>(engineRelative.Target);
			var processMs      = (int) (EntityManager.GetComponentData<RhythmEngineLocalState>(engineRelative.Target).Elapsed.Ticks / TimeSpan.TicksPerMillisecond);
			var beatIntervalMs = (int) (EntityManager.GetComponentData<RhythmEngineSettings>(engineRelative.Target).BeatInterval.Ticks / TimeSpan.TicksPerMillisecond);

			var canBeTransitioned = commandState.IsInputActive(processMs, beatIntervalMs);
			if (!currAnim.AllowOverride || currAnim.Type != SystemType && canBeTransitioned)
				return;

			InjectAnimationWithSystemData<SystemData>();

			animation.SetTargetAnimation(new TargetAnimation(SystemType, allowOverride: AllowOverride, stopAt: animation.RootTime + 0.25));

			ref var systemData = ref animation.GetSystemData<SystemData>(SystemType);
			if (abilityState.UpdateVersion != systemData.ActivationId)
			{
				systemData.ActivationId = abilityState.UpdateVersion;
				systemData.StartTime    = animation.RootTime;
			}
		}

		protected virtual void OnAbilityUpdate(UnitVisualAnimation animation, ref SystemData systemData)
		{
			systemData.StartTime = animation.RootTime;
		}
		
		protected virtual void PlayableOnInitialize(PlayableInnerCall behavior)
		{
			AnimationMap.Resolve(AnimationMap.First(), new );
			
			var clipPlayable = AnimationClipPlayable.Create(behavior.Graph, clipMap.First().Value);
			behavior.Mixer.AddInput(clipPlayable, 0, 1);
		}

		protected virtual void PlayablePrepareFrame(PlayableInnerCall behavior, Playable playable, FrameData info, ref SystemData systemData)
		{
			var global   = (float) behavior.Root.GetTime() - systemData.StartTime;
			var currAnim = behavior.Visual.CurrAnimation;

			behavior.Mixer.SetTime(global);

			systemData.Weight = 0;
			if (currAnim.CanBlend(behavior.Root.GetTime()) && currAnim.PreviousType == SystemType)
				systemData.Weight = currAnim.GetTransitionWeightFixed(behavior.Root.GetTime());
			else if (currAnim.Type == SystemType)
				systemData.Weight = 1;

			behavior.Root.SetInputWeight(VisualAnimation.GetIndexFrom(behavior.Root, behavior.Self), systemData.Weight);
		}

		public struct Handle
		{
		}

		public struct AbilityClip : IAbilityAnimClip
		{
			public string        Key  { get; set; }
			public AnimationClip Clip { get; set; }
		}

		public struct SystemData
		{
			public float          Weight;
			public int            ActivationId;
			public double         StartTime;
		}

		void IAbilityPlayableSystemCalls.OnInitialize(PlayableInnerCall behavior)
		{
			PlayableOnInitialize(behavior);
		}

		void IAbilityPlayableSystemCalls.PrepareFrame(PlayableInnerCall behavior, Playable playable, FrameData info)
		{
			PlayablePrepareFrame(behavior, playable, info, ref behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType));
		}
	}
}