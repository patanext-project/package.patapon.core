using System;
using System.Collections.Generic;
using Patapon4TLB.Default;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Patapon4TLB.Core
{
	public struct TargetAnimation
	{
		/// <summary>
		/// The system type of the animation
		/// </summary>
		public Type Type;

		/// <summary>
		/// Can other animations override this one?
		/// (There can be some exceptions where some animations can override on some conditions or if it's urgent)
		/// </summary>
		public bool AllowOverride;
	}

	public class UnitVisualPresentation : RuntimeAssetPresentation<UnitVisualPresentation>
	{
		public Animator Animator;
	}

	public class UnitVisualAnimation : MonoBehaviour
	{
		public class PlayableBehavior : PlayableBehaviour
		{
			private PlayableGraph graph => Playable.GetGraph();

			public Playable               Playable;
			public AnimationMixerPlayable RootMixer;

			public override void OnPlayableCreate(Playable playable)
			{
				Playable = playable;
				Playable.SetInputCount(1);

				RootMixer = AnimationMixerPlayable.Create(graph);
				graph.Connect(RootMixer, 0, Playable, 0);
				
				Playable.SetInputWeight(0, 1);
			}
		}

		private abstract class SystemDataBase
		{
			public Type Type;
			public int  Index;
		}

		private class SystemData<T> : SystemDataBase
			where T : struct
		{
			public T               Data;
			public RemoveSystem<T> RemoveDelegate;
		}

		public struct ManageData
		{
			public UnitVisualAnimation Handle;
			public PlayableGraph       Graph;
			public PlayableBehavior    Behavior;
			public int                 Index;
		}

		public delegate void AddSystem<T>(ref ManageData data, ref T systemData) where T : struct;

		public delegate void RemoveSystem<in T>(ManageData data, T systemData) where T : struct;

		public UnitVisualBackend      Backend      { get; private set; }
		public UnitVisualPresentation Presentation { get; private set; }

		public TargetAnimation CurrAnimation { get; private set; } = new TargetAnimation {AllowOverride = true};

		private Dictionary<Type, SystemDataBase> m_SystemData = new Dictionary<Type, SystemDataBase>();
		private PlayableBehavior             m_Playable;
		private AnimationMixerPlayable       m_RootMixer;
		private PlayableGraph                m_PlayableGraph;

		private void OnDestroy()
		{
			m_PlayableGraph.Destroy();
		}

		public void OnBackendSet(UnitVisualBackend backend)
		{
			Backend = backend;

			m_PlayableGraph = PlayableGraph.Create("UnitVisualPresentation.PlayableGraph." + backend.DstEntity);
		}

		public void OnPresentationSet(UnitVisualPresentation presentation)
		{
			Presentation = presentation;

			m_PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
			m_PlayableGraph.Play();
			m_Playable = ScriptPlayable<PlayableBehavior>.Create(m_PlayableGraph).GetBehaviour();
			//AnimationPlayableUtilities.Play(Presentation.Animator, m_Playable.Playable, m_PlayableGraph);
			var output = AnimationPlayableOutput.Create(m_PlayableGraph, "Standard Output", presentation.Animator);
			output.SetSourcePlayable(m_Playable.Playable);
		}

		public void ForceAnimationTarget(TargetAnimation target)
		{
			CurrAnimation = target;
		}

		public void TriggerAnimationClip(int keyId)
		{
			if (Presentation == null)
				return;

			Presentation.Animator.SetTrigger(keyId);
		}

		public bool ContainsSystem(Type type)
		{
			return m_SystemData.ContainsKey(type);
		}

		public void InsertSystem<T>(Type type, AddSystem<T> addDelegate, RemoveSystem<T> removeDelegate)
			where T : struct
		{
			var data = new ManageData
			{
				Handle   = this,
				Behavior = m_Playable,
				Index    = m_SystemData.Count,
				Graph    = m_PlayableGraph
			};
			var systemData = new T();

			addDelegate(ref data, ref systemData);

			m_SystemData[type] = new SystemData<T>
			{
				Data           = systemData,
				Index          = m_SystemData.Count,
				Type           = type,
				RemoveDelegate = removeDelegate
			};
		}

		public ref T GetSystemData<T>(Type type)
			where T : struct
		{
			return ref ((SystemData<T>) m_SystemData[type]).Data;
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
		}

		protected override void Update()
		{
			if (DstEntityManager == null || DstEntityManager.IsCreated && DstEntityManager.Exists(DstEntity))
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

			Entities.ForEach((Transform transform, UnitVisualBackend backend) => { transform.position = EntityManager.GetComponentData<Translation>(backend.DstEntity).Value; });
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(UpdateUnitVisualBackendSystem))]
	public class UnitPressureAnimationSystem : ComponentSystem
	{
		private struct SystemData
		{
			public AnimationMixerPlayable Mixer;

			private int m_CurrentKey;
			public int CurrentKey
			{
				get => m_CurrentKey;
				set
				{
					m_CurrentKey = value;
					Debug.Log("> " + value);
				}
			}
		}

		private EntityQuery                                                     m_PressureEventQuery;
		private EntityQueryBuilder.F_CC<UnitVisualBackend, UnitVisualAnimation> m_ForEachDelegate;

		private const string AddrKey = "char_anims/{0}.anim";

		private NativeArray<PressureEvent> m_PressureEvents;

		private AnimationClip[] m_AnimationClips = new AnimationClip[0];

		private readonly int[] KeyAnimTrigger = new[]
		{
			-1,
			Animator.StringToHash("Pata"),
			Animator.StringToHash("Pon"),
			Animator.StringToHash("Don"),
			Animator.StringToHash("Chaka")
		};

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PressureEventQuery = GetEntityQuery(typeof(PressureEvent));
			m_ForEachDelegate    = ForEach;

			m_AnimationClips = new AnimationClip[4];
			Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Pata")).Completed += op => m_AnimationClips[0] = op.Result;
			Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Pon")).Completed += op => m_AnimationClips[1] = op.Result;
			Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Don")).Completed += op => m_AnimationClips[2] = op.Result;
			Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Chaka")).Completed += op => m_AnimationClips[3] = op.Result;
		}

		protected override void OnUpdate()
		{
			using (m_PressureEvents = m_PressureEventQuery.ToComponentDataArray<PressureEvent>(Allocator.TempJob))
			{
				Entities.WithAll<UnitVisualBackend>().ForEach(m_ForEachDelegate);
			}
		}

		private void AddAnimationData(ref UnitVisualAnimation.ManageData data, ref SystemData systemData)
		{
			var rootMixer = data.Behavior.RootMixer;
			var mixer     = AnimationMixerPlayable.Create(data.Graph, 4);

			mixer.SetPropagateSetTime(true);
			for (var i = 0; i != m_AnimationClips.Length; i++)
			{
				var clipPlayable = AnimationClipPlayable.Create(data.Graph, m_AnimationClips[i]);

				data.Graph.Connect(clipPlayable, 0, mixer, i);
			}

			rootMixer.AddInput(mixer, data.Index, 1);

			systemData.Mixer = mixer;
		}

		private void RemoveAnimationData(UnitVisualAnimation.ManageData data, SystemData systemData)
		{
			
		}

		private void ForEach(UnitVisualBackend backend, UnitVisualAnimation animation)
		{
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

			if (!animation.ContainsSystem(GetType()))
			{
				animation.InsertSystem<SystemData>(GetType(), AddAnimationData, RemoveAnimationData);
			}
			
			ref var data = ref animation.GetSystemData<SystemData>(GetType());
			
			animation.ForceAnimationTarget(new TargetAnimation
			{
				Type          = GetType(),
				AllowOverride = true
			});

			var inputCount = data.Mixer.GetInputCount();
			for (var i = 0; i != inputCount; i++)
			{
				data.Mixer.SetInputWeight(i, i == lastPressure.Key - 1 ? 1 : 0);
			}

			data.Mixer.SetTime(0);
			data.CurrentKey = lastPressure.Key;
		}
	}
}