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
	public class JumpAbilityClientAnimationSystem : GameBaseSystem
	{
		private class SystemPlayable : PlayableBehaviour
		{
			public Playable               Self;
			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public double StartTime;
			public float  Weight;

			public AnimationCurve Curve;

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
				
				//Curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.125f, 1), new Keyframe(0.25f, 1), new Keyframe(0.5f, 0));
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = 1 - VisualAnimation.GetWeightFixed(Root.GetTime(), StartTime, StartTime + 2);

				/*Mixer.SetInputWeight(0, Curve.Evaluate(global));
				Mixer.SetInputWeight(1, Curve.Evaluate(global));
				Mixer.SetInputWeight(2, Curve.Evaluate(global));*/
				Mixer.SetInputWeight(0, global < 0.2f ? 1 : 0);
				Mixer.SetInputWeight(1, global >= 0.2f && global < 0.5f ? 1 : 0);
				Mixer.SetInputWeight(2, global >= 0.5f ? 1 : 0);

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}
		}

		private struct SystemData
		{
			public ScriptPlayable<SystemPlayable> Playable;
			public SystemPlayable                 Behaviour;
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

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_AsyncOperationModule);
			GetModule(out m_AbilityModule);

			m_SystemType = GetType();
			m_ForEachUpdateAnimationDelegate = ForEachUpdateAnimation;

			const int arrayLength = 3;
			for (var i = 0; i != arrayLength; i++)
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
				}

				m_AsyncOperationModule.Add(Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, $"{key}")), new OperationData
				{
					ArrayIndex = i
				});
			}
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOperationModule.Handles.Count; i++)
			{
				var (handle, data) = m_AsyncOperationModule.Get<AnimationClip, OperationData>(i);
				if (handle.Result == null)
					continue;

				if (m_AnimationClips == null)
					m_AnimationClips = new AnimationClip[3];

				m_AnimationClips[data.ArrayIndex] = handle.Result;

				m_AsyncOperationModule.Handles.RemoveAtSwapBack(i);
				i--;
			}

			if (m_AnimationClips == null)
				return;
			
			Entities.ForEach(m_ForEachUpdateAnimationDelegate);
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(playable, data.Index, data.Graph, data.Behavior.RootMixer, m_AnimationClips);

			systemData.Playable  = playable;
			systemData.Behaviour = behavior;
		}

		private void RemoveAnimation(VisualAnimation.ManageData data, SystemData systemData)
		{

		}

		private float nextJump = 0;

		private void ForEachUpdateAnimation(UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			if (Input.GetKeyDown(KeyCode.J))
				nextJump = Time.time + 0.25f;
			
			if (animation.CurrAnimation == new TargetAnimation(m_SystemType) && animation.RootTime > animation.CurrAnimation.StopAt)
			{
				animation.SetTargetAnimation(TargetAnimation.Null);

				animation.GetSystemData<SystemData>(m_SystemType).Behaviour.Weight = 0;
			}

			if (nextJump < 0 || Time.time < nextJump)
				return;

			nextJump = -1;

			if (!animation.ContainsSystem(m_SystemType))
			{
				animation.InsertSystem<SystemData>(m_SystemType, AddAnimation, RemoveAnimation);
			}

			ref var data   = ref animation.GetSystemData<SystemData>(m_SystemType);
			var     stopAt = animation.RootTime + 2;
			animation.SetTargetAnimation(new TargetAnimation(m_SystemType, false, false, stopAt: stopAt));

			data.Behaviour.StartTime = animation.RootTime;
			data.Behaviour.Mixer.SetTime(0);
			data.Behaviour.Weight    = 1;
		}
	}
}