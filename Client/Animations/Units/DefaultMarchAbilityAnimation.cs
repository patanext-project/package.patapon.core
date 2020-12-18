using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Components;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.GamePlay.Units;
using PataNext.Module.Simulation.GameBase.Physics.Components;
using PataNext.CoreAbilities.Mixed.Defaults;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class DefaultMarchAbilityAnimation : BaseAbilityAnimationSystem, IAbilityPlayableSystemCalls
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

		protected override AnimationMap GetAnimationMap()
		{
			return new AnimationMap<(TargetType target, SubType sub)>("DefaultMarchAbility/Animations/")
			{
				{"IdleNormal", (TargetType.Idle, SubType.Normal)},
				{"IdleFerocious", (TargetType.Idle, SubType.Ferocious)},
				{"WalkingNormal", (TargetType.Walking, SubType.Normal)},
				{"WalkingFerocious", (TargetType.Walking, SubType.Ferocious)},
			};
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			if (!EntityManager.TryGetComponentData<AnimationIdleTime>(targetEntity, out var idleTime))
				EntityManager.AddComponentData(targetEntity, new AnimationIdleTime());

			var currAnim = animation.CurrAnimation;
			if (currAnim.Type != SystemType && !currAnim.AllowOverride)
				return;

			var abilityEntity = AbilityFinder.GetAbility(backend.DstEntity);
			if (abilityEntity == default && currAnim.Type == SystemType || currAnim.CanStartAnimationAt(animation.RootTime))
				animation.SetTargetAnimationWithTypeKeepTransition(null);

			InjectAnimationWithSystemData<SystemData>();

			ref var systemData  = ref animation.GetSystemData<SystemData>(SystemType);
			var     doAnimation = currAnim == TargetAnimation.Null || currAnim.Type == SystemType;

			var abilityActive = false;
			if (abilityEntity != default)
			{
				var abilityState             = EntityManager.GetComponentData<AbilityState>(abilityEntity);
				doAnimation |= abilityActive = abilityState.Phase == EAbilityPhase.ActiveOrChaining;
			}

			if (abilityActive)
				systemData.ForceAnimation = true;

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

			systemData.TargetAnimation    = targetAnimation;
			systemData.SubTargetAnimation = subType;

			if (!doAnimation)
				return;

			animation.SetTargetAnimation(new TargetAnimation(SystemType, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd, previousType: currAnim.PreviousType));
		}

		protected override IAbilityPlayableSystemCalls GetPlayableCalls()
		{
			return this;
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultMarchAbility), typeof(Owner));
		}

		public struct SystemData
		{
			public Dictionary<TargetType, Dictionary<SubType, AnimationClipPlayable>> ClipPlayableMap;
			public Dictionary<TargetType, AnimationMixerPlayable>                     MixerMap;

			public double LastWalk;

			public bool ForceAnimation;

			public TargetType PreviousAnimation;
			public TargetType TargetAnimation;
			public SubType    SubTargetAnimation;

			public Transition FromTransition;
			public Transition ToTransition;
		}

		void IAbilityPlayableSystemCalls.OnInitialize(PlayableInnerCall behavior)
		{
			var systemData = behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);
			systemData.ClipPlayableMap = new Dictionary<TargetType, Dictionary<SubType, AnimationClipPlayable>>
			{
				{TargetType.Idle, new Dictionary<SubType, AnimationClipPlayable>()},
				{TargetType.Walking, new Dictionary<SubType, AnimationClipPlayable>()}
			};
			systemData.MixerMap = new Dictionary<TargetType, AnimationMixerPlayable>
			{
				{TargetType.Idle, AnimationMixerPlayable.Create(behavior.Graph, 2, true)},
				{TargetType.Walking, AnimationMixerPlayable.Create(behavior.Graph, 2, true)}
			};
			behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType) = systemData;

			foreach (var kvp in ((AnimationMap<(TargetType target, SubType sub)>) AnimationMap).KeyDataMap)
			{
				behavior.AddAsyncOp(AnimationMap.Resolve(kvp.Key, GetCurrentClipProvider()), handle =>
				{
					var cp = AnimationClipPlayable.Create(behavior.Graph, ((Task<AnimationClip>) handle).Result);
					if (cp.IsNull())
						throw new InvalidOperationException("null clip");

					behavior.Graph.Connect(cp, 0, systemData.MixerMap[kvp.Value.target], (int) kvp.Value.sub);
				});
			}

			behavior.Mixer.AddInput(systemData.MixerMap[TargetType.Idle], 0, 1);
			behavior.Mixer.AddInput(systemData.MixerMap[TargetType.Walking], 0, 1);
		}

		void IAbilityPlayableSystemCalls.PrepareFrame(PlayableInnerCall bv, Playable playable, FrameData info)
		{
			const float TRANSITION_TIME = 0.2f;

			ref var data   = ref bv.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);
			var     global = (float) bv.Root.GetTime();

			var weight = 1 - bv.Visual.CurrAnimation.GetTransitionWeightFixed(bv.Visual.VisualAnimation.RootTime);
			if (bv.Visual.CurrAnimation.Type != typeof(DefaultMarchAbilityAnimation) && !bv.Visual.CurrAnimation.CanBlend(global))
				weight = 0;

			if ((weight >= 1) & (data.PreviousAnimation != data.TargetAnimation))
			{
				var offset = 0f;
				if (data.PreviousAnimation == TargetType.Walking           // walking
				&& data.ClipPlayableMap[data.PreviousAnimation].Count > 0) // it's possible that there are no animation at the moment
				{
					var clipPlayable = (AnimationClipPlayable) data
					                                           .MixerMap[data.PreviousAnimation]
					                                           .GetInput((int) data.SubTargetAnimation % data.ClipPlayableMap[data.PreviousAnimation].Count);
					if (!clipPlayable.IsNull())
					{
						var length = clipPlayable.GetAnimationClip().length;
						var mod    = clipPlayable.GetTime() % length;

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
				}

				data.PreviousAnimation = data.TargetAnimation;

				data.ToTransition.End(global + offset, global + offset + TRANSITION_TIME);
				data.FromTransition.Begin(global + offset, global + offset + TRANSITION_TIME);
				data.FromTransition.End(global + offset, global + offset + TRANSITION_TIME);
			}

			if (data.ForceAnimation)
			{
				data.ForceAnimation = false;

				data.PreviousAnimation = data.TargetAnimation;

				data.ToTransition.End(0, 0);
				data.FromTransition.Begin(0, 0);
				data.FromTransition.End(0, 0);
			}

			foreach (var kvp in data.MixerMap)
			{
				var isNormal = data.SubTargetAnimation == SubType.Normal || !data.ClipPlayableMap[kvp.Key].ContainsKey(SubType.Ferocious);
				kvp.Value.SetInputWeight((int) SubType.Normal, isNormal ? 1 : 0);
				kvp.Value.SetInputWeight((int) SubType.Ferocious, !isNormal ? 1 : 0);
			}

			// why does the mixer report 4 inputs???
			var inputCount = bv.Mixer.GetInputCount();
			for (var i = 0; i != inputCount; i++)
			{
				bv.Mixer.SetInputWeight(i, i == (int) data.TargetAnimation
					? data.FromTransition.Evaluate(global, 0, 1)
					: data.ToTransition.Evaluate(global));
			}

			bv.Root.SetInputWeight(VisualAnimation.GetIndexFrom(bv.Root, bv.Self), weight);
		}
	}
}