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
		private class SystemPlayable : PlayableBehaviour
		{
			public Playable               Self;
			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public double StartTime;
			public float  Weight;

			public Transition StartTransition;
			public Transition UpTransition;
			public Transition AirTransition;

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
				UpTransition    = new Transition(StartTransition, 0.4f, 0.5f);
				AirTransition   = new Transition(UpTransition, 1.8f, 2f);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = (float) (Root.GetTime() - StartTime);

				Mixer.SetInputWeight(0, StartTransition.Evaluate(global));
				Mixer.SetInputWeight(1, UpTransition.Evaluate(global));
				Mixer.SetInputWeight(2, AirTransition.Evaluate(global));

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

			m_SystemType                     = GetType();
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

		private void ForEachUpdateAnimation(UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			if (animation.CurrAnimation == new TargetAnimation(m_SystemType) && animation.RootTime > animation.CurrAnimation.StopAt)
			{
				animation.SetTargetAnimation(TargetAnimation.Null);

				animation.GetSystemData<SystemData>(m_SystemType).Behaviour.Weight = 0;
			}

			if (!Input.GetKeyDown(KeyCode.J))
				return;

			Debug.Break();
			
			if (!animation.ContainsSystem(m_SystemType))
			{
				animation.InsertSystem<SystemData>(m_SystemType, AddAnimation, RemoveAnimation);
			}

			ref var data   = ref animation.GetSystemData<SystemData>(m_SystemType);
			var     stopAt = animation.RootTime + 3.5f;
			animation.SetTargetAnimation(new TargetAnimation(m_SystemType, false, false, stopAt: stopAt));

			data.Behaviour.StartTime = animation.RootTime;
			data.Behaviour.Mixer.SetTime(0);
			data.Behaviour.Weight = 1;
		}
	}
}