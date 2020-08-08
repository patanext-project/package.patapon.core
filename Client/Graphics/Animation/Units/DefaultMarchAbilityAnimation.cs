using System;
using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Components;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.GamePlay.Units;
using PataNext.Module.Simulation.GameBase.Physics.Components;
using PataNext.Simulation.Mixed.Abilities.Defaults;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class DefaultMarchAbilityAnimation : BaseAbilityAnimationSystem
	<
		DefaultMarchAbilityAnimation.SystemPlayable,
		DefaultMarchAbilityAnimation.PlayableInitData,
		DefaultMarchAbilityAnimation.SystemData,
		DefaultMarchAbilityAnimation.OperationHandleData,
		DefaultMarchAbilityAnimation.Sub
	>
	{
		public enum SubType
		{
			Normal    = 0,
			Ferocious = 1
		}

		public enum TargetType
		{
			Idle    = 0,
			Walking = 1
		}

		private readonly AddressBuilderClient m_AddrPath = AddressBuilder.Client()
		                                                                 .Folder("Models")
		                                                                 .Folder("UberHero")
		                                                                 .Folder("Animations")
		                                                                 .Folder("Shared");

		private Dictionary<TargetType, Dictionary<Sub, AnimationClip>> m_Clips;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Clips = new Dictionary<TargetType, Dictionary<Sub, AnimationClip>>();
			PreLoadAnimationAsset(m_AddrPath.GetFile("Idle.anim"), new OperationHandleData
			{
				Key  = "marchAbility/idle.clip",
				type = TargetType.Idle,
				sub  = SubType.Normal
			});
			PreLoadAnimationAsset(m_AddrPath.GetFile("Walking.anim"), new OperationHandleData
			{
				Key  = "marchAbility/walking.clip",
				type = TargetType.Walking,
				sub  = SubType.Normal
			});
			PreLoadAnimationAsset(m_AddrPath.GetFile("WalkingFerocious.anim"), new OperationHandleData
			{
				Key  = "marchAbility/walking_ferocious.clip",
				type = TargetType.Walking,
				sub  = SubType.Ferocious
			});
		}

		protected override void OnAsyncOpElement(OperationHandleData data, Sub result)
		{
			if (!m_Clips.TryGetValue(data.type, out var map))
				m_Clips[data.type] = map = new Dictionary<Sub, AnimationClip>();

			result.sub  = data.sub;
			map[result] = result.Clip;
		}

		protected override void OnAnimationInject(UnitVisualAnimation animation, ref PlayableInitData initData)
		{
			initData.Clips = new Dictionary<TargetType, Dictionary<Sub, AnimationClip>>();

			foreach (var kvp in m_Clips)
			{
				initData.Clips[kvp.Key] = new Dictionary<Sub, AnimationClip>(m_Clips[kvp.Key]);
				SetAnimOverride(CurrentPresentation.GetComponent<OverrideObjectComponent>(), kvp.Value, initData.Clips[kvp.Key]);
			}
		}

		protected override bool OnBeforeForEach()
		{
			return base.OnBeforeForEach() && AsyncOp.Handles.Count == 0;
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			if (!EntityManager.TryGetComponentData<AnimationIdleTime>(targetEntity, out var idleTime)) EntityManager.AddComponentData(targetEntity, new AnimationIdleTime());

			var currAnim = animation.CurrAnimation;
			if (currAnim.Type != SystemType && !currAnim.AllowOverride)
			{
				if (animation.ContainsSystem(SystemType))
					animation.GetSystemData<SystemData>(SystemType).Behaviour.Weight = 0;

				return;
			}

			var abilityEntity = AbilityFinder.GetAbility(backend.DstEntity);
			if (abilityEntity == default && currAnim.Type == SystemType || currAnim.CanStartAnimationAt(animation.RootTime))
				animation.SetTargetAnimationWithTypeKeepTransition(null);

			// OnAnimationInject will fill the Init data.
			InjectAnimation(animation, new PlayableInitData());


			ref var systemData  = ref animation.GetSystemData<SystemData>(SystemType);
			var     doAnimation = currAnim == TargetAnimation.Null || currAnim.Type == SystemType;

			var abilityActive = false;
			if (abilityEntity != default)
			{
				var abilityState             = EntityManager.GetComponentData<AbilityState>(abilityEntity);
				doAnimation |= abilityActive = abilityState.Phase == EAbilityPhase.ActiveOrChaining;
			}

			if (abilityActive)
				systemData.Behaviour.ForceAnimation = true;

			var velocity        = EntityManager.GetComponentData<Velocity>(backend.DstEntity);
			var targetAnimation = math.abs(velocity.Value.x) > 0.1f || abilityActive ? TargetType.Walking : TargetType.Idle;
			if (targetAnimation == TargetType.Walking)
				systemData.LastWalk = Time.ElapsedTime;
			else if (systemData.LastWalk + 1.0f > Time.ElapsedTime || idleTime.Value < 2.5f)
				targetAnimation = TargetType.Walking;

			var subType = SubType.Normal;
			if (EntityManager.TryGetComponentData(targetEntity, out UnitEnemySeekingState seekingState)
			    && seekingState.Enemy != default)
				subType = SubType.Ferocious;

			systemData.Behaviour.TargetAnimation    = targetAnimation;
			systemData.Behaviour.SubTargetAnimation = subType;

			if (!doAnimation)
				return;

			animation.SetTargetAnimation(new TargetAnimation(SystemType, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd, previousType: currAnim.PreviousType));
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultMarchAbility), typeof(Owner));
		}

		public struct Sub : IAbilityAnimClip
		{
			public SubType sub;

			public string        Key  { get; set; }
			public AnimationClip Clip { get; set; }
		}

		public struct PlayableInitData
		{
			public Dictionary<TargetType, Dictionary<Sub, AnimationClip>> Clips;
		}

		public class SystemPlayable : BaseAbilityPlayable<PlayableInitData>
		{
			public const float TRANSITION_TIME = 0.2f;

			public bool ForceAnimation; // no transition

			public  Transition                                                         FromTransition;
			private Dictionary<TargetType, Dictionary<SubType, AnimationClipPlayable>> m_ClipPlayableMap;
			private Dictionary<TargetType, AnimationMixerPlayable>                     m_MixerMap;
			private TargetType                                                         m_PreviousAnimation;
			public  SubType                                                            SubTargetAnimation;

			public TargetType TargetAnimation;
			public Transition ToTransition;
			public float      Weight;

			protected override void OnInitialize(PlayableInitData init)
			{
				m_ClipPlayableMap = new Dictionary<TargetType, Dictionary<SubType, AnimationClipPlayable>>();
				m_MixerMap        = new Dictionary<TargetType, AnimationMixerPlayable>();

				foreach (var kvp in init.Clips)
				{
					m_ClipPlayableMap[kvp.Key] = new Dictionary<SubType, AnimationClipPlayable>();

					var mixer = AnimationMixerPlayable.Create(Graph, init.Clips.Count, true);
					var i     = 0;
					foreach (var subKvp in kvp.Value)
					{
						var cp = AnimationClipPlayable.Create(Graph, subKvp.Value);
						if (cp.IsNull())
							throw new InvalidOperationException("null clip");

						Graph.Connect(cp, 0, mixer, (int) subKvp.Key.sub);
						m_ClipPlayableMap[kvp.Key][subKvp.Key.sub] = cp;
					}

					m_MixerMap[kvp.Key] = mixer;
					Mixer.AddInput(mixer, 0, 1);
				}
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = (float) Root.GetTime();

				if ((Weight >= 1) & (m_PreviousAnimation != TargetAnimation))
				{
					var offset = 0f;
					if (m_PreviousAnimation == TargetType.Walking) // walking
					{
						var clipPlayable = (AnimationClipPlayable) m_MixerMap[m_PreviousAnimation].GetInput((int) SubTargetAnimation % m_ClipPlayableMap[m_PreviousAnimation].Count);
						var length       = clipPlayable.GetAnimationClip().length;
						var mod          = clipPlayable.GetTime() % length;

						// For transitions, we need to see when to start the next animation, and we need to be sure to start at zero
						// 1.
						// - We first check if the modulo result is bigger than the length of the playing animation, and if it is, reduce it by the modulo result
						// - This will make the current animation end in a better state. (If there wasn't this, we could have ended from
						//   where the UberHero would have lift all its legs and it would look weird when transitionning into the idle animation)
						if (mod > length * 0.5f)
							offset += length - (float) mod;
						else
							offset += (float) mod;

						// 2.
						// - Remove /4 of the offset, if it's less than 0, we add back /4 of the length to ensure it does start and finish in a good stance
						// - It should be near of (TRANSITION_TIME)
						offset -= length * 0.25f;
						if (offset < 0)
							offset += length * 0.25f;
					}

					m_PreviousAnimation = TargetAnimation;

					ToTransition.End(global + offset, global + offset + TRANSITION_TIME);
					FromTransition.Begin(global + offset, global + offset + TRANSITION_TIME);
					FromTransition.End(global + offset, global + offset + TRANSITION_TIME);
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
				for (var i = 0; i != inputCount; i++) Mixer.SetInputWeight(i, i == (int) TargetAnimation ? FromTransition.Evaluate(global, 0, 1) : ToTransition.Evaluate(global));

				Weight = 1 - Visual.CurrAnimation.GetTransitionWeightFixed(Visual.VisualAnimation.RootTime);
				if (Visual.CurrAnimation.Type != typeof(DefaultMarchAbilityAnimation) && !Visual.CurrAnimation.CanBlend(Visual.RootTime))
					Weight = 0;

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}
		}

		public struct SystemData : IPlayableSystemData<SystemPlayable>
		{
			public double LastWalk;

			public SystemPlayable Behaviour { get; set; }
		}

		public struct OperationHandleData : IAbilityAnimationKey
		{
			public TargetType type;
			public SubType    sub;

			public string Key { get; set; }
		}
	}
}