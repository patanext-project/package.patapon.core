using System;
using System.Collections.Generic;
using package.patapon.core.Animation;
using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Patapon4TLB.Default
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class JumpAbilityClientAnimationSystem : GameBaseSystem
	{
		private enum Phase
		{
			Jumping,
			Idle,
			Fall
		}

		private class SystemPlayable : PlayableBehaviour
		{
			private enum AnimationType
			{
				Start   = 0,
				Jump    = 1,
				IdleAir = 2,
				Fall    = 3
			}

			public Playable                        Self;
			public UnitVisualPlayableBehaviourData VisualData;

			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public double StartTime;
			public Phase  Phase;

			private Phase m_PreviousPhase;

			public float Weight;

			public Transition StartTransition;
			public Transition JumpTransition;

			public Transition IdleAirTransition1;
			public Transition IdleAirTransition2;
			
			public Transition FallTransition1;
			public Transition FallTransition2;

			public void Initialize(Playable self, int index, PlayableGraph graph, AnimationMixerPlayable rootMixer, IReadOnlyList<AnimationClip> clips)
			{
				Self = self;
				Root = rootMixer;

				Mixer = AnimationMixerPlayable.Create(graph, clips.Count, true);
				Mixer.SetPropagateSetTime(true);
				for (var i = 0; i != clips.Count; i++)
				{
					var clipPlayable = AnimationClipPlayable.Create(graph, clips[i]);

					Mixer.ConnectInput(i, clipPlayable, 0);
				}

				rootMixer.AddInput(self, 0, 1);
				self.AddInput(Mixer, 0, 1);

				StartTransition = new Transition(clips[0], 0.75f, 1f);
				JumpTransition  = new Transition(StartTransition, 0.4f, 0.5f);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = (float) (Root.GetTime() - StartTime);

				if (m_PreviousPhase != Phase)
				{
					if (m_PreviousPhase == Phase.Jumping && Phase == Phase.Idle)
					{
						IdleAirTransition1.End(global, global + 0.1f);
						IdleAirTransition2.Begin(global, global + 0.1f);
						IdleAirTransition2.End(global + 0.1f, global + 0.1f);
					}

					if (m_PreviousPhase == Phase.Idle && Phase == Phase.Fall)
					{
						FallTransition1.End(global, global + 0.1f);
						FallTransition2.Begin(global, global + 0.1f);
						FallTransition2.End(global, global + 0.1f);
					}

					m_PreviousPhase = Phase;
				}

				Mixer.SetInputWeight((int) AnimationType.Start, 0);
				Mixer.SetInputWeight((int) AnimationType.Jump, 0);
				Mixer.SetInputWeight((int) AnimationType.IdleAir, 0);
				Mixer.SetInputWeight((int) AnimationType.Fall, 0);
				switch (Phase)
				{
					case Phase.Jumping:
						Mixer.SetInputWeight((int) AnimationType.Start, StartTransition.Evaluate(global));
						Mixer.SetInputWeight((int) AnimationType.Jump, JumpTransition.Evaluate(global, 0, 1));
						break;
					case Phase.Idle:
						Mixer.SetInputWeight((int) AnimationType.Jump, IdleAirTransition1.Evaluate(global));
						Mixer.SetInputWeight((int) AnimationType.IdleAir, IdleAirTransition2.Evaluate(global, 0, 1));
						break;
					case Phase.Fall:
						Mixer.SetInputWeight((int) AnimationType.IdleAir, FallTransition1.Evaluate(global));
						Mixer.SetInputWeight((int) AnimationType.Fall, FallTransition2.Evaluate(global, 0, 1));
						break;
				}

				var currAnim = VisualData.CurrAnimation;
				var systemType = typeof(JumpAbilityClientAnimationSystem);

				Weight = 0;
				if (currAnim.CanBlend(Root.GetTime()) && currAnim.PreviousType == systemType)
				{
					Weight = currAnim.GetTransitionWeightFixed(Root.GetTime());
				}
				else if (currAnim.Type == systemType)
				{
					Weight = 1;
				}

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}
		}

		private struct SystemData
		{
			public SystemPlayable                 Behaviour;

			public int    ActiveId;
			public double StartAt;
		}

		private struct OperationData
		{
			public int ArrayIndex;
		}

		private const string          AddrKey = "char_anims/Jump/Jump{0}.anim";
		private       AnimationClip[] m_AnimationClips;

		private AsyncOperationModule m_AsyncOperationModule;
		private SystemAbilityModule  m_AbilityModule;

		private EntityQueryBuilder.F_CC<UnitVisualBackend, UnitVisualAnimation> m_ForEachUpdateAnimationDelegate;

		private Type m_SystemType;

		private const int ArrayLength = 4;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_AsyncOperationModule);
			GetModule(out m_AbilityModule);

			m_AbilityModule.Query = GetEntityQuery(typeof(JumpAbility), typeof(Owner));

			m_SystemType                     = GetType();
			m_ForEachUpdateAnimationDelegate = ForEachUpdateAnimation;

			for (var i = 0; i != ArrayLength; i++)
			{
				var key = "Start";
				switch (i)
				{
					case 0:
						key = "Start";
						break;
					case 1:
						key = "Idle";
						break;
					case 2:
						key = "IdleAir";
						break;
					case 3:
						key = "Fall";
						break;
				}

				m_AsyncOperationModule.Add(Addressables.LoadAsset<AnimationClip>(string.Format(AddrKey, $"{key}")), new OperationData
				{
					ArrayIndex = i
				});
			}
		}

		private int m_LoadSuccess = 0;

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOperationModule.Handles.Count; i++)
			{
				var (handle, data) = m_AsyncOperationModule.Get<AnimationClip, OperationData>(i);
				if (handle.Result == null)
					continue;

				if (m_AnimationClips == null)
					m_AnimationClips = new AnimationClip[ArrayLength];

				m_AnimationClips[data.ArrayIndex] = handle.Result;
				m_LoadSuccess++;

				m_AsyncOperationModule.Handles.RemoveAtSwapBack(i);
				i--;
			}

			if (m_LoadSuccess != ArrayLength)
				return;

			m_AbilityModule.Update(default).Complete();
			Entities.ForEach(m_ForEachUpdateAnimationDelegate);
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(playable, data.Index, data.Graph, data.Behavior.RootMixer, m_AnimationClips);
			
			systemData.ActiveId             = -1;
			systemData.StartAt              = -1;
			systemData.Behaviour            = behavior;
			systemData.Behaviour.VisualData = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		private void RemoveAnimation(VisualAnimation.ManageData data, SystemData systemData)
		{

		}

		private void ForEachUpdateAnimation(UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type == m_SystemType && animation.RootTime > currAnim.StopAt)
			{
				// allow transitions and overrides now...
				animation.SetTargetAnimation(new TargetAnimation(currAnim.Type, transitionStart: currAnim.StopAt, transitionEnd: currAnim.StopAt + 0.25f));
				// if no one set another animation, then let's set to null...
				if (animation.RootTime > currAnim.StopAt + 0.25f)
					animation.SetTargetAnimation(TargetAnimation.Null);
			}

			var abilityEntity = m_AbilityModule.GetAbility(backend.DstEntity);
			if (abilityEntity == default)
				return;

			if (!animation.ContainsSystem(m_SystemType))
			{
				animation.InsertSystem<SystemData>(m_SystemType, AddAnimation, RemoveAnimation);
			}

			var abilityState = EntityManager.GetComponentData<RhythmAbilityState>(abilityEntity);
			var jumpAbility  = EntityManager.GetComponentData<JumpAbility>(abilityEntity);
			var velocity     = EntityManager.GetComponentData<Velocity>(backend.DstEntity);

			if (!abilityState.IsStillChaining && !abilityState.IsActive && !abilityState.WillBeActive)
			{
				if (currAnim.Type == m_SystemType)
					animation.SetTargetAnimation(TargetAnimation.Null);

				return;
			}

			ref var data = ref animation.GetSystemData<SystemData>(m_SystemType);


			if (abilityState.WillBeActive && data.StartAt < 0 && abilityState.ActiveId >= data.ActiveId && !abilityState.IsActive)
			{
				var serverTime = ServerTick.Ms;
				var delay      = math.max(abilityState.StartTime - 200 - serverTime, 0) * 0.001f;
				// StartTime - StartJump Animation Approx Length in ms - Time, aka delay 0.2s before the command
				
				Debug.Log($"{abilityState.StartTime - serverTime} {delay}");
 
				data.StartAt  = animation.RootTime + delay;
				data.ActiveId = abilityState.ActiveId + 1;
			}

			// Start animation if Behavior.ActiveId and Jump.ActiveId is different... or if we need to start now
			if (abilityState.IsActive && abilityState.ActiveId != data.ActiveId || data.StartAt > 0 && data.StartAt < data.Behaviour.Root.GetTime())
			{
				var stopAt = animation.RootTime + 3.5f;
				animation.SetTargetAnimation(new TargetAnimation(m_SystemType, false, false, stopAt: stopAt));
				
				Debug.Log("Start Animation");


				data.StartAt = -1;
				if (abilityState.IsActive)
					data.ActiveId = abilityState.ActiveId;
				data.Behaviour.StartTime = animation.RootTime;
				data.Behaviour.Mixer.SetTime(0);
				data.Behaviour.Weight = 1;
			}

			var targetPhase = Phase.Idle;
			if (jumpAbility.IsJumping || abilityState.WillBeActive)
			{
				targetPhase = Phase.Jumping;
			}
			else if (velocity.Value.y < 0)
				targetPhase = Phase.Fall;

			data.Behaviour.Phase = targetPhase;
		}
	}
}