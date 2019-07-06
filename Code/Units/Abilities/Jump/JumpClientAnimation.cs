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
			public struct AnimTransition
			{
				public float Key0;
				public float Key1;
				public float Key2;
				public float Key3;
				
				public AnimTransition(AnimationClip clip, float pg1, float pg2, float pg3)
				{
					Key0 = clip.length * pg1;
					Key1 = clip.length * pg1;
					Key2 = clip.length * pg2;
					Key3 = clip.length * pg3;
				}

				public AnimTransition(AnimationClip clip, float pg0, float pg1, float pg2, float pg3)
				{
					Key0 = clip.length * pg0;
					Key1 = clip.length * pg1;
					Key2 = clip.length * pg2;
					Key3 = clip.length * pg3;
				}

				public AnimTransition(AnimTransition left, AnimationClip clip, float pg2, float pg3)
				{
					Key0 = left.Key2;
					Key1 = left.Key3;
					Key2 = clip.length * pg2 + Key0;
					Key3 = clip.length * pg3 + Key0;
				}

				public void Begin(float key0, float key1)
				{
					Key0 = key0;
					Key1 = key1;
				}

				public void End(float key2, float key3)
				{
					Key2 = key2;
					Key3 = key3;
				}

				public float Evaluate(float time, float offset = 0)
				{
					time -= offset;
					if (time > Key3)
						return 0;
					if (time <= Key0)
						return 0;
					if (time <= Key1)
						return math.unlerp(Key0, Key1, time);
					if (time >= Key2 && time <= Key3)
						return math.unlerp(Key3, Key2, time);
					return 1;
				}
			}

			public Playable               Self;
			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public double StartTime;
			public float  Weight;

			/*public AnimationCurve StartCurve;
			public AnimationCurve UpCurve;
			public AnimationCurve AirCurve;
			public AnimationCurve DownCurve;*/
			public AnimTransition StartTransition;
			public AnimTransition UpTransition;
			public AnimTransition AirTransition;

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

				//StartCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.25f, 1), new Keyframe(0.4f, 0));
				//UpCurve = new AnimationCurve(new Keyframe(0.25f, 0), new Keyframe(0.4f, 1), new Keyframe(0.75f, 1), new Keyframe(1f, 0));
				//AirCurve = new AnimationCurve(new Keyframe(0.75f, 0), new Keyframe(1f, 1), new Keyframe(3f, 1), new Keyframe(4f, 0));
				StartTransition = new AnimTransition(clips[0], 0, 0, 0.75f, 1f);
				UpTransition    = new AnimTransition(StartTransition, clips[1], 0.75f, 1f);
				AirTransition   = new AnimTransition(UpTransition, clips[2], 2f, 2.5f);

				for (var i = 0.0f; i < 1.25f; i += 0.05f)
				{
					Debug.Log($"t={i:F2} --> {StartTransition.Evaluate(i):F2}; {UpTransition.Evaluate(i):F2}; {AirTransition.Evaluate(i):F2}");
				}
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = (float) (Root.GetTime() - StartTime);

				/*Mixer.SetInputWeight(0, Evaluate(global, StartCurve));
				Mixer.SetInputWeight(1, Evaluate(global, UpCurve));
				Mixer.SetInputWeight(2, Evaluate(global, AirCurve));*/
				Mixer.SetInputWeight(0, StartTransition.Evaluate(global));
				Mixer.SetInputWeight(1, UpTransition.Evaluate(global));
				Mixer.SetInputWeight(2, AirTransition.Evaluate(global));

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}

			private float Evaluate(float time, AnimationCurve curve)
			{
				if (time > curve.keys[curve.keys.Length - 1].time)
					return 0;
				if (time < curve.keys[0].time)
					return 0;
				return curve.Evaluate(time);
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