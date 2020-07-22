using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	public struct AnimationIdleTime : IComponentData
	{
		public float Value;

		public class System : SystemBase
		{
			protected override void OnUpdate()
			{
				var dt = Time.DeltaTime;
				Entities.ForEach((ref AnimationIdleTime idleTime) =>
				{
					idleTime.Value += dt;
					if (idleTime.Value < 0)
						idleTime.Value = 0;
				}).Schedule();
			}
		}
	}

	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class MarchAbilityClientAnimationSystem : BaseAbilityAnimationSystem
	{
		private const string AddrKey = "core://Client/Models/UberHero/Animations/Shared/{0}.anim";

		private Dictionary<TargetType, Dictionary<Sub, AnimationClip>> m_Clips;
		private int             m_LoadSuccess;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Clips = new Dictionary<TargetType, Dictionary<Sub, AnimationClip>>();

			LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrKey, "Idle"), new OperationHandleData
			{
				key  = "marchAbility/idle.clip",
				type = TargetType.Idle,
				sub  = SubType.Normal
			});
			LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrKey, "Walking"), new OperationHandleData
			{
				key  = "marchAbility/walking.clip",
				type = TargetType.Walking,
				sub  = SubType.Normal
			});
			LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrKey, "WalkingFerocious"), new OperationHandleData
			{
				key  = "marchAbility/walking_ferocious.clip",
				type = TargetType.Walking,
				sub  = SubType.Ferocious
			});
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultMarchAbility), typeof(Owner));
		}

		protected override void OnAsyncOpUpdate(ref int index)
		{
			var (handle, data) = DefaultAsyncOperation.InvokeExecute<AnimationClip, OperationHandleData>(AsyncOp, ref index);
			if (handle.Result == null)
				return;

			if (!m_Clips.TryGetValue(data.type, out var map))
				m_Clips[data.type] = map = new Dictionary<Sub, AnimationClip>();

			map[new Sub {sub = data.sub, key = data.key}] = handle.Result;
			m_LoadSuccess++;
		}

		protected override bool OnBeforeForEach()
		{
			if (!base.OnBeforeForEach())
				return false;

			return m_LoadSuccess >= 3;
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			var clips = new Dictionary<TargetType, Dictionary<Sub, AnimationClip>>(m_Clips);
			var objOverride = CurrentPresentation.GetComponent<OverrideObjectComponent>();
			foreach (var kvp in m_Clips)
			{
				clips[kvp.Key] = new Dictionary<Sub, AnimationClip>(m_Clips[kvp.Key]);
				foreach (var clip in kvp.Value)
				{
					var replaced = clip.Value;
					if (objOverride != null)
						objOverride.TryGetPresentationObject(clip.Key.key, out replaced, clip.Value);
					clips[kvp.Key][clip.Key] = replaced;
				}
			}

			behavior.Initialize(playable, data.Index, data.Graph, data.Behavior.RootMixer, clips);

			systemData.Playable             = playable;
			systemData.Behaviour            = behavior;
			systemData.Behaviour.VisualData = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		private void RemoveAnimation(VisualAnimation.ManageData manageData, SystemData systemData)
		{
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			if (!EntityManager.TryGetComponentData<AnimationIdleTime>(targetEntity, out var idleTime))
			{
				EntityManager.AddComponentData<AnimationIdleTime>(targetEntity, new AnimationIdleTime());
			}
			
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type != SystemType && !currAnim.AllowOverride)
			{
				if (animation.ContainsSystem(SystemType)) animation.GetSystemData<SystemData>(SystemType).Behaviour.Weight = 0;

				return;
			}

			var abilityEntity = AbilityFinder.GetAbility(backend.DstEntity);
			if (abilityEntity == default && currAnim.Type == SystemType || currAnim.CanStartAnimationAt(animation.RootTime)) animation.SetTargetAnimation(new TargetAnimation(null, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd, previousType: currAnim.Type));

			if (!animation.ContainsSystem(SystemType)) animation.InsertSystem<SystemData>(SystemType, AddAnimation, RemoveAnimation);

			ref var systemData  = ref animation.GetSystemData<SystemData>(SystemType);
			var doAnimation = currAnim == TargetAnimation.Null || currAnim.Type == SystemType;

			var abilityActive = false;
			if (abilityEntity != default)
			{
				var abilityState             = EntityManager.GetComponentData<AbilityState>(abilityEntity);
				doAnimation |= abilityActive = abilityState.Phase == EAbilityPhase.ActiveOrChaining;
			}

			if (abilityActive) systemData.Behaviour.ForceAnimation = true;

			var velocity = EntityManager.GetComponentData<SVelocity>(backend.DstEntity);
			var targetAnimation = math.abs(velocity.Value.x) > 0f || abilityActive ? TargetType.Walking : TargetType.Idle;
			if (targetAnimation == TargetType.Walking)
				systemData.LastWalk = Time.ElapsedTime;
			else if (systemData.LastWalk + 1.0f > Time.ElapsedTime || idleTime.Value < 2.5f)
				targetAnimation = TargetType.Walking;

			var subType = SubType.Normal;
			if (EntityManager.TryGetComponentData(targetEntity, out UnitEnemySeekingState seekingState)
			    && seekingState.Enemy != default)
			{
				subType = SubType.Ferocious;
			}

			systemData.Behaviour.TargetAnimation = targetAnimation;
			systemData.Behaviour.SubTargetAnimation = subType;

			if (!doAnimation)
				return;

			animation.SetTargetAnimation(new TargetAnimation(SystemType, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd, previousType: currAnim.PreviousType));
		}

		public enum TargetType
		{
			Idle = 0,
			Walking = 1
		}

		public struct Sub
		{
			public string  key;
			public SubType sub;
		}

		public enum SubType
		{
			Normal = 0,
			Ferocious = 1
		}

		private class SystemPlayable : PlayableBehaviour
		{
			public  bool       ForceAnimation; // no transition
			public  Transition FromTransition;
			private TargetType m_PreviousAnimation;

			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public Playable   Self;
			public TargetType TargetAnimation;
			public SubType    SubTargetAnimation;

			public Transition                      ToTransition;
			public UnitVisualPlayableBehaviourData VisualData;
			public float                           Weight;

			private Dictionary<TargetType, Dictionary<SubType, AnimationClipPlayable>> m_ClipPlayableMap;
			private Dictionary<TargetType, AnimationMixerPlayable> m_MixerMap;

			public void Initialize(Playable self, int index, PlayableGraph graph, AnimationMixerPlayable rootMixer, Dictionary<TargetType, Dictionary<Sub, AnimationClip>> clips)
			{
				Self = self;
				Root = rootMixer;

				Mixer = AnimationMixerPlayable.Create(graph, 0, true);
				Mixer.SetPropagateSetTime(true);
				
				m_ClipPlayableMap = new Dictionary<TargetType, Dictionary<SubType, AnimationClipPlayable>>();
				m_MixerMap        = new Dictionary<TargetType, AnimationMixerPlayable>();

				var mixerCount = 0;
				foreach (var kvp in clips)
				{
					m_ClipPlayableMap[kvp.Key] = new Dictionary<SubType, AnimationClipPlayable>();

					var mixer = AnimationMixerPlayable.Create(graph, clips.Count, true);
					var i     = 0;
					foreach (var subKvp in kvp.Value)
					{
						var cp = AnimationClipPlayable.Create(graph, subKvp.Value);
						graph.Connect(cp, 0, mixer, (int) subKvp.Key.sub);
						m_ClipPlayableMap[kvp.Key][subKvp.Key.sub] = cp;
					}

					m_MixerMap[kvp.Key] = mixer;
					Mixer.AddInput(mixer, 0, 1);
				}

				rootMixer.AddInput(self, 0, 1);
				self.AddInput(Mixer, 0, 1);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = (float) Root.GetTime();

				if ((Weight >= 1) & (m_PreviousAnimation != TargetAnimation))
				{
					var offset = 0f;
					if (m_PreviousAnimation == TargetType.Walking) // walking
					{
						var clipPlayable = (AnimationClipPlayable) m_MixerMap[m_PreviousAnimation].GetInput(((int) SubTargetAnimation) % m_ClipPlayableMap[m_PreviousAnimation].Count);
						var length       = clipPlayable.GetAnimationClip().length;
						var mod          = clipPlayable.GetTime() % length;
						if (mod > length * 0.5f)
							offset += length - (float) mod;
						else
							offset += (float) mod;

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
				
				foreach (var kvp in m_MixerMap)
				{
					kvp.Value.SetInputWeight((int) SubType.Normal, SubTargetAnimation == SubType.Normal || !m_ClipPlayableMap[kvp.Key].ContainsKey(SubType.Ferocious) ? 1 : 0);
					kvp.Value.SetInputWeight((int) SubType.Ferocious, SubTargetAnimation == SubType.Ferocious ? 1 : 0);
				}
				
				// why does the mixer report 4 inputs???
				var inputCount = Mixer.GetInputCount();
				for (var i = 0; i != inputCount; i++)
				{
					Mixer.SetInputWeight(i, i == (int) TargetAnimation ? FromTransition.Evaluate(global, 0, 1) : ToTransition.Evaluate(global));
				}

				Weight = 1 - VisualData.CurrAnimation.GetTransitionWeightFixed(VisualData.VisualAnimation.RootTime);
				if (VisualData.CurrAnimation.Type != typeof(MarchAbilityClientAnimationSystem) && !VisualData.CurrAnimation.CanBlend(VisualData.RootTime)) Weight = 0;

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}
		}

		private struct SystemData
		{
			public ScriptPlayable<SystemPlayable> Playable;
			public SystemPlayable                 Behaviour;
			public double LastWalk;
		}

		private struct OperationHandleData
		{
			public string key;
			public TargetType type;
			public SubType sub;
		}
	}
}