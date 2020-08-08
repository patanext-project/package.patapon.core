using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.GameBase.Physics.Components;
using PataNext.Simulation.Mixed.Abilities.Defaults;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateBefore(typeof(DefaultMarchAbilityAnimation))]
	[UpdateBefore(typeof(UnitPressureClientAnimation))]
	public class DefaultJumpAbilityAnimation : BaseCompleteAbilityAnimationSystem
	<
		DefaultJumpAbilityAnimation.OperationHandle,
		DefaultJumpAbilityAnimation.AbilityClip,
		DefaultJumpAbilityAnimation.SystemData
	>
	{
		public struct OperationHandle
		{
			public int Index;
		}

		public struct AbilityClip : IAbilityAnimClip
		{
			public string        Key  { get; set; }
			public AnimationClip Clip { get; set; }
			public int           Index;
		}

		public enum Phase
		{
			Jumping,
			Idle,
			Fall
		}

		private enum AnimationType
		{
			Start   = 0,
			Jump    = 1,
			IdleAir = 2,
			Fall    = 3
		}

		public struct SystemData : IPlayableSystemData<PlayableSystem>
		{
			public int            ActiveId;
			public PlayableSystem Behaviour { get; set; }

			public Phase PreviousPhase;
			public Phase Phase;

			public double StartAt;
			public double BehaviourStartTime;

			public Transition StartTransition;

			public Transition FallTransition1;
			public Transition FallTransition2;

			public Transition IdleAirTransition1;
			public Transition IdleAirTransition2;
			public Transition JumpTransition;
			
			public float Weight;
		}

		private readonly AddressBuilderClient address = AddressBuilder.Client()
		                                                              .Folder("Models")
		                                                              .Folder("UberHero")
		                                                              .Folder("Animations")
		                                                              .Folder("Shared")
		                                                              .Folder("Jump");

		private       Dictionary<AbilityClip, AnimationClip> clipMap     = new Dictionary<AbilityClip, AnimationClip>();
		private const int                                    ArrayLength = 4;

		protected override void OnCreate()
		{
			base.OnCreate();

			for (var i = 0; i < ArrayLength; i++)
			{
				var key = i switch
				{
					0 => "Start",
					1 => "Idle",
					2 => "IdleAir",
					3 => "Fall",
					_ => throw new NotImplementedException()
				};
				PreLoadAnimationAsset(address.GetFile($"Jump{key}.anim"), $"JumpAbility/{key.ToLower()}.clip", new OperationHandle
				{
					Index = i
				});
			}
		}

		protected override void OnAsyncOpElement(KeyedHandleData<OperationHandle> handle, AbilityClip result)
		{
			result.Index    = handle.Value.Index;
			clipMap[result] = result.Clip;
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
				return;

			InjectAnimation(animation);
			
			ref var data = ref animation.GetSystemData<SystemData>(SystemType);

			var abilityState = EntityManager.GetComponentData<AbilityState>(abilityEntity);
			var jumpAbility  = EntityManager.GetComponentData<DefaultJumpAbility>(abilityEntity);

			GameCommandState commandState;
			uint             processMs;
			float            velocityY;
			if (EntityManager.TryGetComponentData(abilityEntity, out Owner owner))
			{
				velocityY = EntityManager.GetComponentData<Velocity>(owner.Target).Value.y;

				if (!EntityManager.TryGetComponentData(owner.Target, out Relative<RhythmEngineDescription> engineRelative))
					return;

				commandState = EntityManager.GetComponentData<GameCommandState>(engineRelative.Target);
				processMs    = (uint) (EntityManager.GetComponentData<RhythmEngineLocalState>(engineRelative.Target).Elapsed.Ticks / TimeSpan.TicksPerMillisecond);
			}
			else
				return;


			if (abilityState.Phase == EAbilityPhase.None)
			{
				if (currAnim.Type == SystemType)
					animation.SetTargetAnimation(TargetAnimation.Null);

				return;
			}

			ResetIdleTime(targetEntity);

			if ((abilityState.Phase & EAbilityPhase.WillBeActive) != 0 && data.StartAt < 0 && abilityState.UpdateVersion >= data.ActiveId)
			{
				var delay = math.max(commandState.StartTimeMs - 200 - processMs, 0) * 0.001f;
				Console.WriteLine("Delay: " + delay);
				// StartTime - StartJump Animation Approx Length in ms - Time, aka delay 0.2s before the command

				data.StartAt            = animation.RootTime + delay;
				data.ActiveId           = abilityState.UpdateVersion + 1;
			}

			// Start animation if Behavior.ActiveId and Jump.ActiveId is different... or if we need to start now
			if ((abilityState.Phase & EAbilityPhase.Active) != 0 && abilityState.UpdateVersion > data.ActiveId
			    || data.StartAt > 0 && data.StartAt < animation.RootTime)
			{
				var stopAt = animation.RootTime + (commandState.ChainEndTimeMs - processMs) * 0.001f - 1.25f;
				Console.WriteLine($"{stopAt - animation.RootTime} --> {commandState.ChainEndTimeMs} - {processMs}");
				animation.SetTargetAnimation(new TargetAnimation(SystemType, false, false, stopAt: stopAt));

				data.Behaviour.Mixer.SetTime(0);
				data.BehaviourStartTime = animation.RootTime;
				data.StartAt            = -1;

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

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultJumpAbility), typeof(Owner));
		}

		private class Playable__ : PlayableSystem
		{
		}

		protected override ScriptPlayable<PlayableSystem> GetNewPlayable(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			return (ScriptPlayable<PlayableSystem>) (Playable) ScriptPlayable<Playable__>.Create(data.Graph);
		}

		protected override void PlayableOnInitialize(PlayableSystem behavior, ref SystemData systemData)
		{
			systemData.ActiveId = -1;
			systemData.StartAt = -1;
			
			foreach (var clip in clipMap)
			{
				var clipPlayable = AnimationClipPlayable.Create(behavior.Graph, clip.Value);
				var idx          = clip.Key.Index;
				if (behavior.Mixer.GetInputCount() <= idx)
					behavior.Mixer.SetInputCount(idx + 1);

				behavior.Mixer.ConnectInput(idx, clipPlayable, 0);

				Console.WriteLine("Clip Index: " + idx);
				if (idx == 0)
					systemData.JumpTransition = new Transition(systemData.StartTransition = new Transition(clip.Value, 0.75f, 1f), 0.4f, 0.5f);
			}
			
			Console.WriteLine($"{systemData.StartTransition.Key0} {systemData.StartTransition.Key1} {systemData.StartTransition.Key2} {systemData.StartTransition.Key3}");
		}

		protected override void PlayablePrepareFrame(PlayableSystem behavior, Playable playable, FrameData info, ref SystemData systemData)
		{
			var global = (float) (behavior.Root.GetTime() - systemData.BehaviourStartTime);

			if (systemData.PreviousPhase != systemData.Phase)
			{
				switch (systemData.PreviousPhase)
				{
					case Phase.Jumping when systemData.Phase == Phase.Idle:
						const float TRANSITION_TIME = 0.09f;
						
						systemData.IdleAirTransition1.End(global, global + TRANSITION_TIME);
						systemData.IdleAirTransition2.Begin(global, global + TRANSITION_TIME);
						systemData.IdleAirTransition2.End(global + TRANSITION_TIME, global + TRANSITION_TIME);
						break;
					case Phase.Idle when systemData.Phase == Phase.Fall:
						systemData.FallTransition1.End(global, global + 0.1f);
						systemData.FallTransition2.Begin(global, global + 0.1f);
						systemData.FallTransition2.End(global, global + 0.1f);
						break;
				}

				if (global >= 0)
					Console.WriteLine($"{systemData.ActiveId} --> {systemData.PreviousPhase} to {systemData.Phase}");
				
				systemData.PreviousPhase = systemData.Phase;
			}

			behavior.Mixer.SetInputWeight((int) AnimationType.Start, 0);
			behavior.Mixer.SetInputWeight((int) AnimationType.Jump, 0);
			behavior.Mixer.SetInputWeight((int) AnimationType.IdleAir, 0);
			behavior.Mixer.SetInputWeight((int) AnimationType.Fall, 0);
			switch (systemData.Phase)
			{
				case Phase.Jumping:
					behavior.Mixer.SetInputWeight((int) AnimationType.Start, systemData.StartTransition.Evaluate(global));
					behavior.Mixer.SetInputWeight((int) AnimationType.Jump, systemData.JumpTransition.Evaluate(global, 0, 1));
					break;
				case Phase.Idle:
					behavior.Mixer.SetInputWeight((int) AnimationType.Jump, systemData.IdleAirTransition1.Evaluate(global));
					behavior.Mixer.SetInputWeight((int) AnimationType.IdleAir, systemData.IdleAirTransition2.Evaluate(global, 0, 1));
					break;
				case Phase.Fall:
					behavior.Mixer.SetInputWeight((int) AnimationType.IdleAir, systemData.FallTransition1.Evaluate(global));
					behavior.Mixer.SetInputWeight((int) AnimationType.Fall, systemData.FallTransition2.Evaluate(global, 0, 1));
					break;
			}
			
			var currAnim = behavior.Visual.CurrAnimation;

			systemData.Weight = 0;
			if (currAnim.CanBlend(behavior.Root.GetTime()) && currAnim.PreviousType == SystemType)
				systemData.Weight = currAnim.GetTransitionWeightFixed(behavior.Root.GetTime());
			else if (currAnim.Type == SystemType)
				systemData.Weight = 1;

			behavior.Root.SetInputWeight(VisualAnimation.GetIndexFrom(behavior.Root, behavior.Self), systemData.Weight);
		}
	}
}