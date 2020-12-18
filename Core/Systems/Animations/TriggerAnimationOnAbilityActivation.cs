using System;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.Roles;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;

namespace PataNext.Client.Graphics.Animation.Base
{
	public abstract class TriggerAnimationOnAbilityActivation<TAbility> : TriggerAnimationAbilitySystem<TAbility, int>
		where TAbility : struct, IComponentData
	{
		public override EAbilityPhase KeepAnimationAtPhase => EAbilityPhase.Active;

		protected virtual string AnimationClip => "OnActivate";

		protected override string[] GetTriggers()
		{
			return new[] {AnimationClip};
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation, Entity abilityEntity, AbilityState abilityState)
		{
			if (!EntityManager.TryGetComponentData(EntityManager.GetComponentData<Owner>(abilityEntity).Target, out Relative<RhythmEngineDescription> engineRelative))
				return;

			var commandState   = EntityManager.GetComponentData<GameCommandState>(engineRelative.Target);
			var processMs      = (int) (EntityManager.GetComponentData<RhythmEngineLocalState>(engineRelative.Target).Elapsed.Ticks / TimeSpan.TicksPerMillisecond);
			var beatIntervalMs = (int) (EntityManager.GetComponentData<RhythmEngineSettings>(engineRelative.Target).BeatInterval.Ticks / TimeSpan.TicksPerMillisecond);

			var canBeTransitioned = commandState.IsInputActive(processMs, beatIntervalMs);
			if (!animation.CurrAnimation.AllowOverride || animation.CurrAnimation.Type != SystemType && canBeTransitioned)
				return;

			ref var systemData = ref CurrentVisualAnimation.GetSystemData<SystemData>(SystemType);
			if (abilityState.UpdateVersion != systemData.Supplements)
			{
				systemData.Supplements = abilityState.UpdateVersion;
				Trigger(AnimationClip);
			}
			else
				KeepOrTrigger(AnimationClip);
		}
	}

	/*public abstract class TriggerAnimationOnAbilityActivation<TAbility> : BaseAbilityAnimationSystem, IAbilityPlayableSystemCalls
		where TAbility : struct, IComponentData
	{
		public struct SystemData
		{
			public int    ActivationId;
			public double StartTime;
			public float  Weight;
		}

		protected virtual string AnimationPrefix => $"{typeof(TAbility).Name}/Animations/";
		protected virtual string AnimationClip   => "OnActivate";

		public virtual bool          AllowOverride        => true;
		public virtual EAbilityPhase KeepAnimationAtPhase => EAbilityPhase.Active;

		protected override AnimationMap GetAnimationMap()
		{
			return new AnimationMap<string>(AnimationPrefix)
			{
				{AnimationClip, string.Empty}
			};
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

		protected override IAbilityPlayableSystemCalls GetPlayableCalls()
		{
			return this;
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(TAbility));
		}

		void IAbilityPlayableSystemCalls.OnInitialize(PlayableInnerCall behavior)
		{
			behavior.Mixer.SetInputCount(1);
			behavior.AddAsyncOp(AnimationMap.Resolve(AnimationClip, GetCurrentClipProvider()), handle =>
			{
				var cp = AnimationClipPlayable.Create(behavior.Graph, (AnimationClip) handle.Result);
				if (cp.IsNull())
					throw new InvalidOperationException("null clip");

				behavior.Graph.Connect(cp, 0, behavior.Mixer, 0);
			});
		}

		void IAbilityPlayableSystemCalls.PrepareFrame(PlayableInnerCall behavior, Playable playable, FrameData info)
		{
			ref var systemData = ref behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);

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
	}*/
}