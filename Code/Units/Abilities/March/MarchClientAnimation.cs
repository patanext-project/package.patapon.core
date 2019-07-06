using System;
using System.Collections.Generic;
using package.patapon.core.Animation;
using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
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
			public Playable               Self;
			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public int   TargetAnimation;
			public float Weight;

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

				rootMixer.AddInput(self, index, 1);
				self.AddInput(Mixer, 0, 1);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var inputCount = Mixer.GetInputCount();
				for (var i = 0; i != inputCount; i++)
				{
					Mixer.SetInputWeight(i, i == TargetAnimation ? 1 : 0);
					if (i == TargetAnimation)
						Mixer.GetInput(i).Play();
					else
						Mixer.GetInput(i).Pause();
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
			public bool IsAttackAnimation;
		}

		private AnimationClip m_MarchAnimationClip;
		private AnimationClip m_MarchAttackAnimationClip;

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

			m_AsyncOperationModule.Add(Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Walking")), new OperationHandleData {IsAttackAnimation = false});
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOperationModule.Handles.Count; i++)
			{
				var (handle, data) = m_AsyncOperationModule.Get<AnimationClip, OperationHandleData>(i);
				if (handle.Result == null)
					continue;

				if (data.IsAttackAnimation) m_MarchAttackAnimationClip = handle.Result;
				else m_MarchAnimationClip                              = handle.Result;

				m_AsyncOperationModule.Handles.RemoveAtSwapBack(i);
				i--;
			}

			if (m_MarchAnimationClip == null)
				return;


			m_AbilityModule.Update(default).Complete();
			Entities.ForEach(m_ForEachDelegate);
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(playable, data.Index, data.Graph, data.Behavior.RootMixer, new[]
			{
				m_MarchAnimationClip,
				// m_MarchAttackAnimationClip // not yet
			});

			systemData.Playable  = playable;
			systemData.Behaviour = behavior;
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
			
			var abilityEntity = m_AbilityModule.FindFromOwner(backend.DstEntity);
			if (abilityEntity == default && currAnim.Type == m_SystemType || currAnim.CanStartAnimationAt(animation.RootTime))
			{
				animation.SetTargetAnimation(new TargetAnimation(null, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd));
			}

			if (!animation.ContainsSystem(m_SystemType))
			{
				animation.InsertSystem<SystemData>(m_SystemType, AddAnimation, RemoveAnimation);
			}

			var systemData  = animation.GetSystemData<SystemData>(m_SystemType);
			var doAnimation = currAnim == TargetAnimation.Null || currAnim.Type == m_SystemType;

			if (abilityEntity != default)
			{
				var abilityState = EntityManager.GetComponentData<RhythmAbilityState>(abilityEntity);
				doAnimation |= abilityState.IsActive;
			}

			if (doAnimation)
			{
				systemData.Behaviour.TargetAnimation = 0;
				systemData.Behaviour.Weight          = 1 - currAnim.GetTransitionWeightFixed(animation.RootTime);

				animation.SetTargetAnimation(new TargetAnimation(m_SystemType, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd));
			}
			else
			{
				systemData.Behaviour.Weight = 0;
			}
		}
	}
}