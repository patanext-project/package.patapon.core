using System;
using System.Threading.Tasks;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.Defaults;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.GameBase.Physics.Components;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class UnitJumpAbilityAnimation : BaseAbilityAnimationSystem, IAbilityPlayableSystemCalls
	{
		public enum AnimationType
		{
			Start   = 0,
			Jump    = 1,
			IdleAir = 2,
			Fall    = 3,
			Count   = 4
		}

		public enum Phase
		{
			Jumping,
			Idle,
			Fall
		}

		protected override AnimationMap GetAnimationMap()
		{
			return new AnimationMap<AnimationType>("DefaultJumpAbility/Animations/")
			{
				{"Start", AnimationType.Start},
				{"Jump", AnimationType.Jump},
				{"IdleAir", AnimationType.IdleAir},
				{"Fall", AnimationType.Fall},
			};
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type == SystemType && animation.RootTime >= currAnim.StopAt)
			{
				// allow transitions and overrides now...
				animation.SetTargetAnimation(new TargetAnimation(currAnim.Type, transitionStart: currAnim.StopAt, transitionEnd: currAnim.StopAt + 0.25f));
				// if no one set another animation, then let's set to null...
				if (animation.RootTime >= currAnim.StopAt + 0.25f)
					animation.SetTargetAnimation(TargetAnimation.Null);
			}

			var abilityEntity = AbilityFinder.GetAbility(targetEntity);
			if (abilityEntity == default)
			{
				if (animation.ContainsSystem(SystemType))
				{
					ref var temp = ref animation.GetSystemData<SystemData>(SystemType);
					temp.StartTime = -1;
					temp.StartAt   = -1;
					temp.Phase     = default;
					temp.ActiveId  = -1;
				}
				
				return;
			}

			InjectAnimationWithSystemData<SystemData>();
			
			var owner     = EntityManager.GetComponentData<Owner>(abilityEntity);
			var velocityY = EntityManager.GetComponentData<Velocity>(owner.Target).Value.y;

			if (!EntityManager.TryGetComponentData(owner.Target, out Relative<RhythmEngineDescription> engineRelative))
				return;

			var commandState = EntityManager.GetComponentData<GameCommandState>(engineRelative.Target);
			var processMs    = (uint) (EntityManager.GetComponentData<RhythmEngineLocalState>(engineRelative.Target).Elapsed.Ticks / TimeSpan.TicksPerMillisecond);

			var abilityState = EntityManager.GetComponentData<AbilityState>(abilityEntity);
			if (abilityState.Phase == EAbilityPhase.None)
			{
				if (currAnim.Type == SystemType) 
					animation.SetTargetAnimation(TargetAnimation.Null);

				return;
			}

			var jumpAbility = EntityManager.GetComponentData<DefaultJumpAbility>(abilityEntity);
			ResetIdleTime(targetEntity);

			ref var data = ref animation.GetSystemData<SystemData>(SystemType);
			if ((abilityState.Phase & EAbilityPhase.WillBeActive) != 0 && data.StartAt < 0 && abilityState.UpdateVersion >= data.ActiveId)
			{
				var delay = math.max(commandState.StartTimeMs - 200 - processMs, 0) * 0.001f;
				// StartTime - StartJump Animation Approx Length in ms - Time, aka delay 0.2s before the command

				data.StartAt  = animation.RootTime + delay;
				data.ActiveId = abilityState.UpdateVersion + 1;
			}

			// Start animation if Behavior.ActiveId and Jump.ActiveId is different... or if we need to start now
			if ((abilityState.Phase & EAbilityPhase.Active) != 0 && abilityState.UpdateVersion > data.ActiveId
			    || data.StartAt > 0 && data.StartAt < animation.RootTime)
			{
				var stopAt = animation.RootTime + (commandState.ChainEndTimeMs - processMs) * 0.001f - 1.1f;
				animation.SetTargetAnimation(new TargetAnimation(SystemType, false, false, stopAt: stopAt));

				data.bv.Mixer.SetTime(0);
				data.StartTime = animation.RootTime;
				data.StartAt   = -1;

				if (abilityState.Phase == EAbilityPhase.Active)
					data.ActiveId = abilityState.UpdateVersion;

				data.Weight = 1;
			}

			var targetPhase = Phase.Idle;
			if (jumpAbility.IsJumping || (abilityState.Phase & EAbilityPhase.WillBeActive) != 0)
				targetPhase = Phase.Jumping;
			else if (velocityY < 0)
				targetPhase = Phase.Fall;

			data.Phase = targetPhase;
		}

		protected override IAbilityPlayableSystemCalls GetPlayableCalls()
		{
			return this;
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultJumpAbility), typeof(Owner));
		}

		public struct SystemData
		{
			public AnimationClip[] AnimationClips;

			public Transition FallTransition1;
			public Transition FallTransition2;

			public Transition IdleAirTransition1;
			public Transition IdleAirTransition2;
			public Transition JumpTransition;

			public Phase PreviousPhase;

			public Phase Phase;
			
			public double StartTime;

			public Transition StartTransition;

			public float Weight;

			public int    ActiveId;
			public double StartAt;

			public PlayableInnerCall bv;
			public bool              Initialized;
		}

		void IAbilityPlayableSystemCalls.OnInitialize(PlayableInnerCall behavior)
		{
			var systemData = behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);
			systemData.AnimationClips = new AnimationClip[(int) AnimationType.Count];
			systemData.bv             = behavior;
			systemData.ActiveId       = -1;
			systemData.StartAt        = -1;

			behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType) = systemData;

			behavior.Mixer.SetInputCount(systemData.AnimationClips.Length);
			foreach (var kvp in ((AnimationMap<AnimationType>) AnimationMap).KeyDataMap)
			{
				behavior.AddAsyncOp(AnimationMap.Resolve(kvp.Key, GetCurrentClipProvider()), handle =>
				{
					var clip = ((Task<AnimationClip>) handle).Result;
					systemData.AnimationClips[(int) kvp.Value] = clip;
					
					var cp = AnimationClipPlayable.Create(behavior.Graph, clip);
					if (cp.IsNull())
						throw new InvalidOperationException("null clip");

					behavior.Graph.Connect(cp, 0, behavior.Mixer, (int) kvp.Value);
				});
			}
		}

		void IAbilityPlayableSystemCalls.PrepareFrame(PlayableInnerCall bv, Playable playable, FrameData info)
		{
			ref var data   = ref bv.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);
			var     global = (float) (bv.Root.GetTime() - data.StartTime);
			
			if (!data.Initialized && data.AnimationClips[0] != null)
			{
				data.Initialized = true;
				
				data.StartTransition = new Transition(data.AnimationClips[0], 0.75f, 1f);
				data.JumpTransition  = new Transition(data.StartTransition, 0.4f, 0.5f);
			}
			
			if (data.PreviousPhase != data.Phase)
			{
				switch (data.PreviousPhase)
				{
					case Phase.Jumping when data.Phase == Phase.Idle:
						const float TRANSITION_TIME = 0.09f;
						
						data.IdleAirTransition1.End(global, global + TRANSITION_TIME);
						data.IdleAirTransition2.Begin(global, global + TRANSITION_TIME);
						data.IdleAirTransition2.End(global + TRANSITION_TIME, global + TRANSITION_TIME);
						break;
					case Phase.Idle when data.Phase == Phase.Fall:
						data.FallTransition1.End(global, global + 0.1f);
						data.FallTransition2.Begin(global, global + 0.1f);
						data.FallTransition2.End(global, global + 0.1f);
						break;
				}

				if (global >= 0)
					Console.WriteLine($"{data.ActiveId} --> {data.PreviousPhase} to {data.Phase}");
				
				data.PreviousPhase = data.Phase;
			}

			bv.Mixer.SetInputWeight((int) AnimationType.Start, 0);
			bv.Mixer.SetInputWeight((int) AnimationType.Jump, 0);
			bv.Mixer.SetInputWeight((int) AnimationType.IdleAir, 0);
			bv.Mixer.SetInputWeight((int) AnimationType.Fall, 0);
			switch (data.Phase)
			{
				case Phase.Jumping:
					bv.Mixer.SetInputWeight((int) AnimationType.Start, data.StartTransition.Evaluate(global));
					bv.Mixer.SetInputWeight((int) AnimationType.Jump, data.JumpTransition.Evaluate(global, 0, 1));
					break;
				case Phase.Idle:
					bv.Mixer.SetInputWeight((int) AnimationType.Jump, data.IdleAirTransition1.Evaluate(global));
					bv.Mixer.SetInputWeight((int) AnimationType.IdleAir, data.IdleAirTransition2.Evaluate(global, 0, 1));
					break;
				case Phase.Fall:
					bv.Mixer.SetInputWeight((int) AnimationType.IdleAir, data.FallTransition1.Evaluate(global));
					bv.Mixer.SetInputWeight((int) AnimationType.Fall, data.FallTransition2.Evaluate(global, 0, 1));
					break;
			}

			var currAnim   = bv.Visual.CurrAnimation;
			var systemType = typeof(UnitJumpAbilityAnimation);

			data.Weight = 0;
			if (currAnim.CanBlend(bv.Root.GetTime()) && currAnim.PreviousType == systemType)
				data.Weight                                   = currAnim.GetTransitionWeightFixed(bv.Root.GetTime());
			else if (currAnim.Type == systemType) data.Weight = 1;

			bv.Root.SetInputWeight(VisualAnimation.GetIndexFrom(bv.Root, bv.Self), data.Weight);
		}
	}
}