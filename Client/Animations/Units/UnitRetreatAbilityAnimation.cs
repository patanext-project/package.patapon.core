using System;
using System.Threading.Tasks;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.CoreAbilities.Mixed.Defaults;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class UnitRetreatAbilityAnimation : BaseAbilityAnimationSystem, IAbilityPlayableSystemCalls
	{
		public enum Phase
		{
			Retreating,
			Stop,
			WalkBack,
			Count
		}

		protected override AnimationMap GetAnimationMap()
		{
			return new AnimationMap<Phase>("DefaultRetreatAbility/Animations/")
			{
				{"Run", Phase.Retreating},
				{"Stop", Phase.Stop},
				{"WalkBack", Phase.WalkBack}
			};
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type == SystemType && animation.RootTime >= currAnim.StopAt)
			{
				// allow transitions and overrides now...
				animation.SetTargetAnimation(new TargetAnimation(currAnim.Type, transitionStart: currAnim.StopAt, transitionEnd: currAnim.StopAt + 0.25f));
				// if no one set another animation, then let's set to null...
				if (animation.RootTime >= currAnim.StopAt + 0.25f)
					animation.SetTargetAnimation(TargetAnimation.Null);
			}

			var abilityEntity = AbilityFinder.GetAbility(targetEntity);
			if (abilityEntity == default)
			{
				if (animation.ContainsSystem(SystemType))
				{
					ref var temp = ref animation.GetSystemData<SystemData>(SystemType);
					temp.StartTime = -1;
					temp.Phase     = default;
					temp.ActiveId  = -1;
				}

				return;
			}

			InjectAnimationWithSystemData<SystemData>();

			var abilityState = EntityManager.GetComponentData<AbilityState>(abilityEntity);
			if (abilityState.Phase == EAbilityPhase.None)
			{
				if (currAnim.Type == SystemType)
					animation.SetTargetAnimation(TargetAnimation.Null);

				return;
			}

			var retreatAbility = EntityManager.GetComponentData<DefaultRetreatAbility>(abilityEntity);
			ResetIdleTime(targetEntity);

			ref var data = ref animation.GetSystemData<SystemData>(SystemType);
			// Start animation if Behavior.ActiveId and Retreat.ActiveId is different
			if ((abilityState.Phase & EAbilityPhase.ActiveOrChaining) != 0 && abilityState.ActivationVersion != data.ActiveId)
			{
				var stopAt = animation.RootTime + 2.75f;
				animation.SetTargetAnimation(new TargetAnimation(SystemType, allowOverride: false, allowTransition: false,
					stopAt: stopAt));

				data.ActiveId  = abilityState.ActivationVersion;
				data.StartTime = animation.RootTime;
				data.Weight    = 1;
				data.bv.Mixer.SetTime(0);
			}

			var targetPhase = Phase.Retreating;
			// stop
			if (retreatAbility.ActiveTime >= 1.75f && retreatAbility.ActiveTime < 3.25f)
				targetPhase                                    = Phase.Stop;
			else if (!retreatAbility.IsRetreating) targetPhase = Phase.WalkBack;

			data.Phase = targetPhase;
		}

		protected override IAbilityPlayableSystemCalls GetPlayableCalls()
		{
			return this;
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultRetreatAbility), typeof(Owner));
		}

		public struct SystemData
		{
			public AnimationClip[] AnimationClips;

			public int ActiveId;

			public Phase PreviousPhase;
			public Phase Phase;

			public double StartTime;

			public Transition RetreatingToStopTransition;
			public Transition StopFromRetreatingTransition;
			public Transition StopToWalkTransition;
			public Transition WalkFromStopTransition;

			public float Weight;

			public PlayableInnerCall bv;
		}

		void IAbilityPlayableSystemCalls.OnInitialize(PlayableInnerCall behavior)
		{
			var systemData = behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);
			systemData.AnimationClips = new AnimationClip[(int) Phase.Count];
			systemData.bv             = behavior;
			systemData.ActiveId       = -1;

			behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType) = systemData;

			behavior.Mixer.SetInputCount(systemData.AnimationClips.Length);
			foreach (var kvp in ((AnimationMap<Phase>) AnimationMap).KeyDataMap)
			{
				behavior.AddAsyncOp(AnimationMap.Resolve(kvp.Key, GetCurrentClipProvider()), handle =>
				{
					var clip = ((Task<AnimationClip>) handle).Result;
					systemData.AnimationClips[(int) kvp.Value] = clip;

					var cp = AnimationClipPlayable.Create(behavior.Graph, clip);
					if (cp.IsNull())
						throw new InvalidOperationException("null clip");

					behavior.Graph.Connect(cp, 0, behavior.Mixer, (int) kvp.Value);
				});
			}
		}

		void IAbilityPlayableSystemCalls.PrepareFrame(PlayableInnerCall bv, Playable playable, FrameData info)
		{
			ref var systemData = ref bv.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);
			var     global     = (float) (bv.Root.GetTime() - systemData.StartTime);

			if (systemData.PreviousPhase != systemData.Phase)
			{
				switch (systemData.PreviousPhase)
				{
					case Phase.Retreating when systemData.Phase == Phase.Stop:
						systemData.RetreatingToStopTransition.End(global, global + 0.15f);
						systemData.StopFromRetreatingTransition.Begin(global, global + 0.15f);
						systemData.StopFromRetreatingTransition.End(global, global + 0.15f);
						break;
					case Phase.Stop when systemData.Phase == Phase.WalkBack:
						systemData.StopToWalkTransition.End(global, global + 0.33f);
						systemData.WalkFromStopTransition.Begin(global, global + 0.33f);
						systemData.WalkFromStopTransition.End(global, global + 0.33f);
						break;
				}

				systemData.PreviousPhase = systemData.Phase;
				bv.Mixer.SetTime(0);
			}

			bv.Mixer.SetInputWeight((int) Phase.Retreating, 0);
			bv.Mixer.SetInputWeight((int) Phase.Stop, 0);
			bv.Mixer.SetInputWeight((int) Phase.WalkBack, 0);
			switch (systemData.Phase)
			{
				case Phase.Retreating:
					bv.Mixer.SetInputWeight((int) Phase.Retreating, 1);
					break;
				case Phase.Stop:
					bv.Mixer.SetInputWeight((int) Phase.Retreating, systemData.RetreatingToStopTransition.Evaluate(global));
					bv.Mixer.SetInputWeight((int) Phase.Stop, systemData.StopFromRetreatingTransition.Evaluate(global, 0, 1));
					break;
				case Phase.WalkBack:
					bv.Mixer.SetInputWeight((int) Phase.Stop, systemData.StopToWalkTransition.Evaluate(global));
					bv.Mixer.SetInputWeight((int) Phase.WalkBack, systemData.WalkFromStopTransition.Evaluate(global, 0, 1));
					break;
			}

			var currAnim = bv.Visual.CurrAnimation;

			systemData.Weight = 0;
			if (currAnim.CanBlend(bv.Root.GetTime()) && currAnim.PreviousType == SystemType)
				systemData.Weight = currAnim.GetTransitionWeightFixed(bv.Root.GetTime());
			else if (currAnim.Type == SystemType)
				systemData.Weight = 1;

			bv.Root.SetInputWeight(VisualAnimation.GetIndexFrom(bv.Root, bv.Self), systemData.Weight);
		}
	}
}