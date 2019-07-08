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
	public class RetreatAbilityClientAnimationSystem : GameBaseSystem
	{
		private enum Phase
		{
			Retreating,
			Stop,
			WalkBack,
		}

		private class SystemPlayable : PlayableBehaviour
		{
			private enum AnimationType
			{
				Retreat  = 0,
				Stop     = 1,
				WalkBack = 2 // walk back will be the normal walk animation
			}

			public Playable                        Self;
			public UnitVisualPlayableBehaviourData VisualData;

			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public double StartTime;
			public Phase  Phase;

			private Phase m_PreviousPhase;

			public float Weight;

			public Transition RetreatingToStopTransition;
			public Transition StopFromRetreatingTransition;
			
			public Transition StopToWalkTransition;
			public Transition WalkFromStopTransition;

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
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = (float) (Root.GetTime() - StartTime);

				if (m_PreviousPhase != Phase)
				{
					if (m_PreviousPhase == Phase.Retreating && Phase == Phase.Stop)
					{
						RetreatingToStopTransition.End(global, global + 0.15f);
						StopFromRetreatingTransition.Begin(global, global + 0.15f);
						StopFromRetreatingTransition.End(global, global + 0.15f);
					}
					
					if (m_PreviousPhase == Phase.Stop && Phase == Phase.WalkBack)
					{
						StopToWalkTransition.End(global, global + 0.33f);
						WalkFromStopTransition.Begin(global, global + 0.33f);
						WalkFromStopTransition.End(global, global + 0.33f);
					}

					m_PreviousPhase = Phase;
					Mixer.SetTime(0);
				}

				Mixer.SetInputWeight((int) AnimationType.Retreat, 0);
				Mixer.SetInputWeight((int) AnimationType.Stop, 0);
				Mixer.SetInputWeight((int) AnimationType.WalkBack, 0);
				switch (Phase)
				{
					case Phase.Retreating:
						Mixer.SetInputWeight((int) AnimationType.Retreat, 1);
						break;
					case Phase.Stop:
						Mixer.SetInputWeight((int) AnimationType.Retreat, RetreatingToStopTransition.Evaluate(global));
						Mixer.SetInputWeight((int) AnimationType.Stop, StopFromRetreatingTransition.Evaluate(global, 0, 1));
						break;
					case Phase.WalkBack:
						Mixer.SetInputWeight((int) AnimationType.Stop, StopToWalkTransition.Evaluate(global));
						Mixer.SetInputWeight((int) AnimationType.WalkBack, WalkFromStopTransition.Evaluate(global, 0, 1));
						break;
				}

				var currAnim   = VisualData.CurrAnimation;
				var systemType = typeof(RetreatAbilityClientAnimationSystem);

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
			public SystemPlayable Behaviour;

			public int ActiveId;
		}

		private struct OperationData
		{
			public int ArrayIndex;
		}

		private const string          AddrRetreatKey = "char_anims/Retreat/Retreat{0}.anim";
		private const string          AddrMarchKey   = "char_anims/Walking.anim";
		private       AnimationClip[] m_AnimationClips;

		private AsyncOperationModule m_AsyncOperationModule;
		private SystemAbilityModule  m_AbilityModule;

		private EntityQueryBuilder.F_CC<UnitVisualBackend, UnitVisualAnimation> m_ForEachUpdateAnimationDelegate;

		private Type m_SystemType;

		private const int ArrayLength = 3;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_AsyncOperationModule);
			GetModule(out m_AbilityModule);

			m_AbilityModule.Query = GetEntityQuery(typeof(RetreatAbility), typeof(Owner));

			m_SystemType                     = GetType();
			m_ForEachUpdateAnimationDelegate = ForEachUpdateAnimation;

			for (var i = 0; i != ArrayLength - 1; i++)
			{
				var key = "Run";
				switch (i)
				{
					case 0:
						key = "Run";
						break;
					case 1:
						key = "Stop";
						break;
				}

				m_AsyncOperationModule.Add(Addressables.LoadAsset<AnimationClip>(string.Format(AddrRetreatKey, $"{key}")), new OperationData
				{
					ArrayIndex = i
				});
			}

			m_AsyncOperationModule.Add(Addressables.LoadAsset<AnimationClip>(AddrMarchKey), new OperationData
			{
				ArrayIndex = ArrayLength - 1
			});
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

			if (m_LoadSuccess < ArrayLength)
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
			systemData.Behaviour            = behavior;
			systemData.Behaviour.VisualData = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		private void RemoveAnimation(VisualAnimation.ManageData data, SystemData systemData)
		{

		}

		private void ForEachUpdateAnimation(UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim == new TargetAnimation(m_SystemType) && animation.RootTime > currAnim.StopAt)
			{
				// allow transitions and overrides now...
				animation.SetTargetAnimation(new TargetAnimation(currAnim.Type, transitionStart: currAnim.StopAt, transitionEnd: currAnim.StopAt + 0.25f));
				// if no one set another animation, then let's set to null...
				if (animation.RootTime > currAnim.StopAt + 0.25f)
					animation.SetTargetAnimation(TargetAnimation.Null);
			}

			var abilityEntity = m_AbilityModule.FindFromOwner(backend.DstEntity);
			if (abilityEntity == default)
				return;

			if (!animation.ContainsSystem(m_SystemType))
			{
				animation.InsertSystem<SystemData>(m_SystemType, AddAnimation, RemoveAnimation);
			}

			var abilityState   = EntityManager.GetComponentData<RhythmAbilityState>(abilityEntity);
			var RetreatAbility = EntityManager.GetComponentData<RetreatAbility>(abilityEntity);
			
			if (!abilityState.IsStillChaining && !abilityState.IsActive && !abilityState.WillBeActive)
			{
				if (currAnim == new TargetAnimation(m_SystemType))
					animation.SetTargetAnimation(TargetAnimation.Null);

				return;
			}

			ref var data = ref animation.GetSystemData<SystemData>(m_SystemType);

			// Start animation if Behavior.ActiveId and Retreat.ActiveId is different
			if (abilityState.IsActive && abilityState.ActiveId != data.ActiveId)
			{
				var stopAt = animation.RootTime + 3.25f;
				animation.SetTargetAnimation(new TargetAnimation(m_SystemType, false, false, stopAt: stopAt));

				Debug.Log("Start Animation");


				data.ActiveId            = abilityState.ActiveId;
				data.Behaviour.StartTime = animation.RootTime;
				data.Behaviour.Mixer.SetTime(0);
				data.Behaviour.Weight = 1;
			}

			var targetPhase = Phase.Retreating;
			// stop
			if (RetreatAbility.ActiveTime >= 1.75f && RetreatAbility.ActiveTime <= 3.25f)
			{
				targetPhase = Phase.Stop;
			}
			else if (!RetreatAbility.IsRetreating)
			{
				targetPhase = Phase.WalkBack;
			}

			data.Behaviour.Phase = targetPhase;
		}
	}
}