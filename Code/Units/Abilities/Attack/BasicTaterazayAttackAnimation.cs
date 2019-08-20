using System;
using package.patapon.core.Animation;
using Patapon4TLB.Core;
using StormiumTeam.GameBase;
using StormiumTeam.Shared.Gen;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Patapon4TLB.Default.Attack
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class BasicTaterazayAttackAnimation : GameBaseSystem
	{
		private class SystemPlayable : PlayableBehaviour
		{
			public Playable                        Self;
			public UnitVisualPlayableBehaviourData VisualData;
			public AnimationMixerPlayable          Mixer;
			public AnimationMixerPlayable          Root;

			public double StartTime;
			public float  Weight;

			public void Initialize(Playable self, int index, PlayableGraph graph, AnimationMixerPlayable rootMixer, AnimationClip clip)
			{
				Self = self;
				Root = rootMixer;

				Mixer = AnimationMixerPlayable.Create(graph, 1, true);
				Mixer.SetPropagateSetTime(true);

				var clipPlayable = AnimationClipPlayable.Create(graph, clip);
				Mixer.ConnectInput(0, clipPlayable, 0, 1);

				rootMixer.AddInput(self, 0, 1);
				self.AddInput(Mixer, 0, 1);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global     = (float) (Root.GetTime() - StartTime);
				var currAnim   = VisualData.CurrAnimation;
				var systemType = typeof(BasicTaterazayAttackAnimation);

				Mixer.SetTime(global);

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
			public uint           PreviousAttackTick;
		}

		private struct OperationData
		{
		}

		private AsyncOperationModule m_AsyncOperationModule;
		private SystemAbilityModule  m_AbilityModule;

		private const string        AddrKey = "tate_anim.uber/BasicAttack.anim";
		private       AnimationClip m_AnimationClip;

		private Type m_SystemType;
		private int  m_LoadSuccess;

		private EntityQuery m_BackendQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_BackendQuery = GetEntityQuery(typeof(UnitVisualBackend), typeof(UnitVisualAnimation));

			GetModule(out m_AsyncOperationModule);
			GetModule(out m_AbilityModule);

			m_AbilityModule.Query = GetEntityQuery(typeof(BasicTaterazayAttackAbility), typeof(Owner));

			m_SystemType = GetType();
			m_AsyncOperationModule.Add(Addressables.LoadAssetAsync<AnimationClip>(AddrKey), new OperationData());
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOperationModule.Handles.Count; i++)
			{
				var (handle, _) = m_AsyncOperationModule.Get<AnimationClip, OperationData>(i);
				if (handle.Result == null)
					continue;

				m_AnimationClip = handle.Result;
				m_LoadSuccess++;

				m_AsyncOperationModule.Handles.RemoveAtSwapBack(i);
				i--;
			}

			if (m_LoadSuccess == 0)
				return;

			m_AbilityModule.Update(default).Complete();

			UnitVisualBackend   backend   = null;
			UnitVisualAnimation animation = null;

			var serverTick = GetTick(false);
			foreach (var _ in this.ToEnumerator_CC(m_BackendQuery, ref backend, ref animation))
			{
				var currAnim = animation.CurrAnimation;
				if (currAnim.Type == m_SystemType && currAnim.StopAt < animation.RootTime)
				{
					// allow transitions and overrides now...
					animation.SetTargetAnimation(new TargetAnimation(currAnim.Type, transitionStart: currAnim.StopAt, transitionEnd: currAnim.StopAt + 0.33));
					// if no one set another animation, then let's set to null...
					if (animation.RootTime > currAnim.StopAt + 0.33)
						animation.SetTargetAnimation(TargetAnimation.Null);
				}

				var abilityEntity = m_AbilityModule.GetAbility(backend.DstEntity);
				if (abilityEntity == default)
					continue;

				var gameTick      = serverTick;
				var attackAbility = EntityManager.GetComponentData<BasicTaterazayAttackAbility>(abilityEntity);
				if (attackAbility.AttackStartTick <= 0)
				{
					continue;
				}

				if (!animation.ContainsSystem(m_SystemType))
				{
					animation.InsertSystem<SystemData>(m_SystemType, AddAnimation, RemoveAnimation);
				}

				ref var systemData = ref animation.GetSystemData<SystemData>(m_SystemType);
				if (attackAbility.AttackStartTick == systemData.PreviousAttackTick)
				{
					continue;
				}

				var aheadStartDifference = UTick.CopyDelta(gameTick, math.max(gameTick.Value - attackAbility.AttackStartTick, 0));
				systemData.PreviousAttackTick  = attackAbility.AttackStartTick;
				systemData.Behaviour.StartTime = animation.RootTime - math.clamp(aheadStartDifference.Seconds, -0.2, 0.2);

				animation.SetTargetAnimation(new TargetAnimation(m_SystemType, allowOverride: false, allowTransition: false, stopAt: animation.RootTime + 0.55));
			}
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(playable, data.Index, data.Graph, data.Behavior.RootMixer, m_AnimationClip);

			systemData.PreviousAttackTick   = 0;
			systemData.Behaviour            = behavior;
			systemData.Behaviour.VisualData = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		private void RemoveAnimation(VisualAnimation.ManageData data, SystemData systemData)
		{
		}
	}
}