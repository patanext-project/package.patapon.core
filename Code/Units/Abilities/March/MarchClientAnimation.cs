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
	public class MarchAbilityClientAnimationSystem : GameBaseSystem
	{
		private class SystemPlayable : PlayableBehaviour
		{
			private int m_PreviousAnimation;
			
			public Playable                        Self;
			public UnitVisualPlayableBehaviourData VisualData;

			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public bool ForceAnimation; // no transition
			public int   TargetAnimation;
			public float Weight;

			public Transition ToTransition;
			public Transition FromTransition;

			public void Initialize(Playable self, int index, PlayableGraph graph, AnimationMixerPlayable rootMixer, IReadOnlyList<AnimationClip> clips)
			{
				Self = self;
				Root = rootMixer;

				Mixer = AnimationMixerPlayable.Create(graph, clips.Count, true);
				Mixer.SetPropagateSetTime(true);

				for (var i = 0; i != clips.Count; i++)
				{
					var clipPlayable = AnimationClipPlayable.Create(graph, clips[i]);

					graph.Connect(clipPlayable, 0, Mixer, i);
				}

				rootMixer.AddInput(self, 0, 1);
				self.AddInput(Mixer, 0, 1);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = (float) Root.GetTime();

				if (Weight >= 1 & m_PreviousAnimation != TargetAnimation)
				{
					var offset = 0f;
					if (m_PreviousAnimation == 1) // walking
					{
						var clipPlayable = (AnimationClipPlayable) Mixer.GetInput(m_PreviousAnimation);
						var length       = clipPlayable.GetAnimationClip().length;
						var mod          = clipPlayable.GetTime() % length;
						if (mod > length * 0.5f)
						{
							offset += length - (float) mod;
						}
						else
						{
							offset += (float) mod;
						}
						
						offset -= length * 0.25f;
						if (offset < 0)
							offset += length * 0.25f;
					}

					m_PreviousAnimation = TargetAnimation;

					ToTransition.End(global + offset, global + offset + 0.2f);
					FromTransition.Begin(global + offset, global + offset + 0.2f);
					FromTransition.End(global + offset, global + offset + 0.2f);
				}

				if (ForceAnimation)
				{
					ForceAnimation = false;
					
					m_PreviousAnimation = TargetAnimation;
					
					ToTransition.End(0, 0);
					FromTransition.Begin(0, 0);
					FromTransition.End(0, 0);
				}
				
				var inputCount = Mixer.GetInputCount();
				for (var i = 0; i != inputCount; i++)
				{
					Mixer.SetInputWeight(i, i == TargetAnimation ? FromTransition.Evaluate(global, 0, 1) : ToTransition.Evaluate(global));
				}

				Weight = 1 - VisualData.CurrAnimation.GetTransitionWeightFixed(VisualData.VisualAnimation.RootTime);
				if (VisualData.CurrAnimation.Type != typeof(MarchAbilityClientAnimationSystem) && !VisualData.CurrAnimation.CanBlend(VisualData.RootTime))
				{
					Weight = 0;
				}

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}
		}

		private struct SystemData
		{
			public ScriptPlayable<SystemPlayable> Playable;
			public SystemPlayable                 Behaviour;
		}

		private struct OperationHandleData
		{
			public int Index;
		}

		private AnimationClip[] m_Clips;
		private int m_LoadSuccess;

		private AsyncOperationModule m_AsyncOperationModule;
		private SystemAbilityModule  m_AbilityModule;

		private EntityQuery                                                     m_MarchAbilitiesQuery;
		private EntityQueryBuilder.F_CC<UnitVisualBackend, UnitVisualAnimation> m_ForEachDelegate;

		private Type m_SystemType;

		private const string AddrKey = "char_anims/{0}.anim";

		protected override void OnCreate()
		{
			base.OnCreate();

			m_MarchAbilitiesQuery = GetEntityQuery(typeof(MarchAbility), typeof(Owner));
			m_ForEachDelegate     = ForEach;

			m_SystemType = GetType();

			GetModule(out m_AsyncOperationModule);
			GetModule(out m_AbilityModule);

			m_AbilityModule.Query = m_MarchAbilitiesQuery;

			m_Clips = new AnimationClip[2];
			
			m_AsyncOperationModule.Add(Addressables.LoadAsset<AnimationClip>(string.Format(AddrKey, "Idle")), new OperationHandleData {Index = 0});
			m_AsyncOperationModule.Add(Addressables.LoadAsset<AnimationClip>(string.Format(AddrKey, "Walking")), new OperationHandleData {Index = 1});
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOperationModule.Handles.Count; i++)
			{
				var (handle, data) = m_AsyncOperationModule.Get<AnimationClip, OperationHandleData>(i);
				if (handle.Result == null)
					continue;

				m_Clips[data.Index] = handle.Result;
				m_LoadSuccess++;

				m_AsyncOperationModule.Handles.RemoveAtSwapBack(i);
				i--;
			}

			if (m_LoadSuccess < m_Clips.Length)
				return;


			m_AbilityModule.Update(default).Complete();
			Entities.ForEach(m_ForEachDelegate);
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(playable, data.Index, data.Graph, data.Behavior.RootMixer, m_Clips);

			systemData.Playable             = playable;
			systemData.Behaviour            = behavior;
			systemData.Behaviour.VisualData = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		private void RemoveAnimation(VisualAnimation.ManageData manageData, SystemData systemData)
		{

		}

		private void ForEach(UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type != m_SystemType && !currAnim.AllowOverride)
			{
				if (animation.ContainsSystem(m_SystemType))
				{
					animation.GetSystemData<SystemData>(m_SystemType).Behaviour.Weight = 0;
				}

				return;
			}

			var abilityEntity = m_AbilityModule.GetAbility(backend.DstEntity);
			if (abilityEntity == default && currAnim.Type == m_SystemType || currAnim.CanStartAnimationAt(animation.RootTime))
			{
				animation.SetTargetAnimation(new TargetAnimation(null, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd, previousType: currAnim.Type));
			}

			if (!animation.ContainsSystem(m_SystemType))
			{
				animation.InsertSystem<SystemData>(m_SystemType, AddAnimation, RemoveAnimation);
			}

			var systemData  = animation.GetSystemData<SystemData>(m_SystemType);
			var doAnimation = currAnim == TargetAnimation.Null || currAnim.Type == m_SystemType;

			var abilityActive       = false;
			var abilityWillBeActive = false;
			if (abilityEntity != default)
			{
				var abilityState = EntityManager.GetComponentData<RhythmAbilityState>(abilityEntity);
				abilityWillBeActive =  abilityState.WillBeActive;
				doAnimation         |= abilityActive = abilityState.IsActive;
			}
			
			if (abilityActive || abilityWillBeActive)
			{
				systemData.Behaviour.ForceAnimation = true;
			}

			var velocity = EntityManager.GetComponentData<Velocity>(backend.DstEntity);
			systemData.Behaviour.TargetAnimation = math.abs(velocity.Value.x) > 0f || abilityWillBeActive || abilityActive ? 1 : 0;

			if (!doAnimation)
				return;

			animation.SetTargetAnimation(new TargetAnimation(m_SystemType, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd, previousType: currAnim.PreviousType));
		}
	}
}