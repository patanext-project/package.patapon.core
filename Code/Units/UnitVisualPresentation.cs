using System;
using System.Collections.Generic;
using System.Text;
using package.patapon.core.Animation;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

namespace Patapon4TLB.Core
{
	public struct TargetAnimation
	{
		public static readonly TargetAnimation Null = new TargetAnimation(null);

		/// <summary>
		/// The system type of the animation
		/// </summary>
		public readonly Type Type;

		/// <summary>
		/// The previous system type before the new animation (used for transition)
		/// </summary>
		public readonly Type PreviousType;
		
		/// <summary>
		/// Can other animations override this one?
		/// (There can be some exceptions where some animations can override on some conditions or if it's urgent)
		/// </summary>
		public readonly bool AllowOverride;

		public readonly bool AllowTransition;

		/// <summary>
		/// The current weight of this animation, use this for transition
		/// </summary>
		public readonly float Weight;

		public readonly double TransitionStart;
		public readonly double TransitionEnd;
		public readonly double StopAt;

		public TargetAnimation(Type type, bool allowOverride = true, bool allowTransition = true, float weight = 0, double transitionStart = -1, double transitionEnd = -1, double stopAt = -1, Type previousType = null)
		{
			Type            = type;
			PreviousType  = previousType;
			AllowOverride   = allowOverride;
			AllowTransition = allowTransition;
			Weight          = weight;
			TransitionStart = transitionStart;
			TransitionEnd   = transitionEnd;
			StopAt          = stopAt;
		}

		public static bool operator ==(TargetAnimation left, TargetAnimation right)
		{
			return left.Type == right.Type;
		}

		public static bool operator !=(TargetAnimation left, TargetAnimation right)
		{
			return left.Type != right.Type;
		}

		public float GetTransitionWeightFixed(double time, float fxd = 1)
		{
			if (TransitionStart < 0 || TransitionEnd < 0)
				return 0;
			if (time > TransitionEnd)
				return 0;
			if (time < TransitionStart)
				return fxd;
			return (float) (1 - math.unlerp(TransitionStart, TransitionEnd, time));
		}

		public bool CanStartAnimationAt(double time)
		{
			if (AllowTransition && TransitionStart >= 0 && time >= TransitionEnd)
				return true;
			return StopAt < 0 || time >= StopAt;
		}

		public bool CanBlend(double time)
		{
			return AllowTransition && TransitionStart >= 0 && time <= TransitionEnd;
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(UpdateUnitVisualBackendSystem))]
	public class ClientUnitAnimationGroup : ComponentSystemGroup
	{
	}

	public class UnitVisualPresentation : RuntimeAssetPresentation<UnitVisualPresentation>
	{
		public Animator Animator;
	}

	public class UnitVisualPlayableBehaviourData : PlayableBehaviorData
	{
		public UnitVisualAnimation VisualAnimation;
		public TargetAnimation     CurrAnimation => VisualAnimation.CurrAnimation;
		public double              RootTime      => VisualAnimation.RootTime;
	}

	public class UnitVisualAnimation : VisualAnimation
	{
		public double                 RootTime     => rootMixer.GetTime();
		public UnitVisualBackend      Backend      { get; private set; }
		public UnitVisualPresentation Presentation { get; private set; }

		public TargetAnimation CurrAnimation { get; private set; } = new TargetAnimation(null);

		public void OnDisable()
		{
			DestroyPlayableGraph();
		}

		public void OnBackendSet(UnitVisualBackend backend)
		{
			Backend = backend;

			DestroyPlayableGraph();
			CreatePlayableGraph($"{backend.DstEntity}");
			CreatePlayable();
		}

		public void OnPresentationSet(UnitVisualPresentation presentation)
		{
			Presentation = presentation;
			SetAnimatorOutput("standard output", presentation.Animator);
		}

		public void SetTargetAnimation(TargetAnimation target)
		{
			CurrAnimation = target;
		}

		public UnitVisualPlayableBehaviourData GetBehaviorData()
		{
			return new UnitVisualPlayableBehaviourData
			{
				DstEntity        = Backend.DstEntity,
				DstEntityManager = Backend.DstEntityManager,
				VisualAnimation  = this
			};
		}
	}

	public class UnitVisualBackend : RuntimeAssetBackend<UnitVisualPresentation>
	{
		public UnitVisualAnimation Animation { get; private set; }

		public override void OnPoolSet()
		{
			(Animation = GetComponent<UnitVisualAnimation>()).OnBackendSet(this);
		}

		public override void OnPresentationSet()
		{
			Animation.OnPresentationSet(Presentation);
			Presentation.Animator.runtimeAnimatorController = null;
		}

		protected override void Update()
		{
			if (DstEntityManager != null && DstEntityManager.IsCreated && DstEntityManager.Exists(DstEntity))
			{
				base.Update();
				return;
			}

			Return(true, true);
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(RenderInterpolationSystem))]
	public class UpdateUnitVisualBackendSystem : ComponentSystem
	{
		protected override void OnUpdate()
		{
			EntityManager.CompleteAllJobs();

			Entities.ForEach((Transform transform, UnitVisualBackend backend) =>
			{
				if (backend.DstEntity == Entity.Null || !EntityManager.Exists(backend.DstEntity) || !EntityManager.HasComponent<Translation>(backend.DstEntity))
					return;
				transform.position = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value;
			});
		}
	}

	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class UnitPressureAnimationSystem : ComponentSystem
	{
		public class SystemPlayable : PlayableBehaviour
		{
			public Playable                        Self;
			public UnitVisualPlayableBehaviourData VisualData;

			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;
			public double                 TransitionStart;
			public double                 TransitionEnd;

			public int CurrentKey;

			public void Initialize(PlayableGraph graph, Playable self, int index, AnimationMixerPlayable rootMixer, AnimationClip[] clips)
			{
				Self = self;
				Root = rootMixer;

				Mixer = AnimationMixerPlayable.Create(graph, 4, true);
				Mixer.SetPropagateSetTime(true);

				for (var i = 0; i != clips.Length; i++)
				{
					var clipPlayable = AnimationClipPlayable.Create(graph, clips[i]);

					graph.Connect(clipPlayable, 0, Mixer, i);
				}

				rootMixer.AddInput(self, 0, 0);
				self.AddInput(Mixer, 0, 1);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var inputCount = Mixer.GetInputCount();
				var e          = VisualAnimation.GetWeightFixed(Root.GetTime(), TransitionStart, TransitionEnd);

				for (var i = 0; i != inputCount; i++)
				{
					Mixer.SetInputWeight(i, i == CurrentKey - 1 ? 1 : 0);
				}

				if (!VisualData.CurrAnimation.AllowTransition && VisualData.CurrAnimation.Type != typeof(UnitPressureAnimationSystem))
				{
					e = 0;
				}

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), e);
			}
		}

		private struct SystemData
		{
			public ScriptPlayable<SystemPlayable> Playable;
			public SystemPlayable                 Behaviour;

			public AnimationMixerPlayable Mixer
			{
				get => Behaviour.Mixer;
			}

			public int CurrentKey
			{
				get => Behaviour.CurrentKey;
				set => Behaviour.CurrentKey = value;
			}
		}

		private EntityQuery                                                     m_PressureEventQuery;
		private EntityQueryBuilder.F_CC<UnitVisualBackend, UnitVisualAnimation> m_ForEachPersistentDelegate;
		private EntityQueryBuilder.F_CC<UnitVisualBackend, UnitVisualAnimation> m_ForEachUpdateAnimationDelegate;

		private const string AddrKey = "char_anims/{0}.anim";

		private NativeArray<PressureEvent> m_PressureEvents;

		private AnimationClip[] m_AnimationClips = new AnimationClip[0];
		private Type            m_SystemType;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PressureEventQuery             = GetEntityQuery(typeof(PressureEvent));
			m_ForEachPersistentDelegate      = ForEachPersistent;
			m_ForEachUpdateAnimationDelegate = ForEachUpdateAnimation;

			m_SystemType = GetType();

			m_AnimationClips                                                                      =  new AnimationClip[4];
			Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Pata")).Completed  += op => m_AnimationClips[0] = op.Result;
			Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Pon")).Completed   += op => m_AnimationClips[1] = op.Result;
			Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Don")).Completed   += op => m_AnimationClips[2] = op.Result;
			Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Chaka")).Completed += op => m_AnimationClips[3] = op.Result;
		}

		protected override void OnUpdate()
		{
			using (m_PressureEvents = m_PressureEventQuery.ToComponentDataArray<PressureEvent>(Allocator.TempJob))
			{
				Entities.WithAll<UnitVisualBackend>().ForEach(m_ForEachUpdateAnimationDelegate);
				Entities.WithAll<UnitVisualBackend>().ForEach(m_ForEachPersistentDelegate);
			}
		}

		private void AddAnimationData(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(data.Graph, playable, data.Index, data.Behavior.RootMixer, m_AnimationClips);

			systemData.Playable             = playable;
			systemData.Behaviour            = behavior;
			systemData.Behaviour.VisualData = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		private void RemoveAnimationData(VisualAnimation.ManageData data, SystemData systemData)
		{
			systemData.Mixer.Destroy();
		}

		private void ForEachPersistent(UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			if (!animation.ContainsSystem(m_SystemType))
				return;

			var currAnim = animation.CurrAnimation;

			ref var data = ref animation.GetSystemData<SystemData>(m_SystemType);
			if (currAnim.Type != m_SystemType && !currAnim.CanBlend(animation.RootTime))
			{
				data.Behaviour.TransitionEnd = -1;
			}
		}

		private void ForEachUpdateAnimation(UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			if (m_AnimationClips.Length == 0)
				return;
			if (m_PressureEvents.Length == 0 || backend.Presentation == null || !animation.CurrAnimation.AllowOverride)
				return;

			var relativeRhythmEngine = EntityManager.GetComponentData<Relative<RhythmEngineDescription>>(backend.DstEntity);
			if (relativeRhythmEngine.Target == default)
				return;

			var lastPressure = default(PressureEvent);
			for (var ev = 0; ev != m_PressureEvents.Length; ev++)
			{
				if (m_PressureEvents[ev].Engine == relativeRhythmEngine.Target)
				{
					lastPressure = m_PressureEvents[ev];
				}
				else if (ev == m_PressureEvents.Length - 1)
					return; // no events found
			}

			if (!animation.ContainsSystem(m_SystemType))
			{
				animation.InsertSystem<SystemData>(m_SystemType, AddAnimationData, RemoveAnimationData);
			}

			ref var data = ref animation.GetSystemData<SystemData>(m_SystemType);

			var transitionStart = m_AnimationClips[lastPressure.Key - 1].length * 0.825f + animation.RootTime;
			var transitionEnd   = m_AnimationClips[lastPressure.Key - 1].length + animation.RootTime;

			animation.SetTargetAnimation(new TargetAnimation(m_SystemType, allowTransition: true, transitionStart: transitionStart, transitionEnd: transitionEnd));
			data.CurrentKey                = lastPressure.Key;
			data.Behaviour.TransitionStart = transitionStart;
			data.Behaviour.TransitionEnd   = transitionEnd;

			data.Mixer.SetTime(0);
			data.Playable.Play();
		}
	}
}