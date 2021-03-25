using System.Collections.Generic;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Simulation.Mixed.Abilities.Defaults;
using StormiumTeam.GameBase.Roles.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateBefore(typeof(DefaultMarchAbilityAnimation))]
	[UpdateBefore(typeof(UnitPressureClientAnimation))]
	public class DefaultRetreatAbilityAnimation : BaseCompleteAbilityAnimationSystem
	<
		DefaultRetreatAbilityAnimation.OperationHandle,
		DefaultRetreatAbilityAnimation.AbilityClip,
		DefaultRetreatAbilityAnimation.SystemData
	>
	{
		public struct OperationHandle
		{
			public int Index;
		}

		public struct AbilityClip : IAbilityAnimClip
		{
			public string        Key  { get; set; }
			public AnimationClip Clip { get; set; }
			public int           Index;
		}

		public enum Phase
		{
			Retreating = 0,
			Stop       = 1,
			WalkBack   = 2
		}

		public struct SystemData : IPlayableSystemData<PlayableSystem>
		{
			public int            ActiveId;
			public PlayableSystem Behaviour { get; set; }

			public Phase PreviousPhase;
			public Phase Phase;

			public double StartTime;

			public Transition RetreatingToStopTransition;
			public Transition StopFromRetreatingTransition;
			public Transition StopToWalkTransition;
			public Transition WalkFromStopTransition;

			public float Weight;
		}

		private readonly AddressBuilderClient address = AddressBuilder.Client()
		                                                              .Folder("Models")
		                                                              .Folder("UberHero")
		                                                              .Folder("Animations")
		                                                              .Folder("Shared");

		private       Dictionary<AbilityClip, AnimationClip> clipMap     = new Dictionary<AbilityClip, AnimationClip>();
		private const int                                    ArrayLength = 3;

		protected override void OnCreate()
		{
			base.OnCreate();

			for (var i = 0; i < ArrayLength - 1; i++)
			{
				var key = i switch
				{
					1 => "Stop",
					_ => "Run"
				};
				PreLoadAnimationAsset(address.Folder("Retreat").GetFile($"Retreat{key}.anim"), $"retreatAbility/{key.ToLower()}.clip", new OperationHandle
				{
					Index = i
				});
			}

			PreLoadAnimationAsset(address.GetFile($"Walking.anim"), "retreatAbility/walk_back.clip", new OperationHandle
			{
				Index = ArrayLength - 1
			});
		}

		protected override void OnAsyncOpElement(KeyedHandleData<OperationHandle> handle, AbilityClip result)
		{
			result.Index    = handle.Value.Index;
			clipMap[result] = result.Clip;
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
				return;

			InjectAnimation(animation);

			var abilityState   = EntityManager.GetComponentData<AbilityState>(abilityEntity);
			var retreatAbility = EntityManager.GetComponentData<DefaultRetreatAbility>(abilityEntity);

			if (abilityState.Phase == EAbilityPhase.None)
			{
				if (currAnim.Type == SystemType)
					animation.SetTargetAnimation(TargetAnimation.Null);

				return;
			}

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
				data.Behaviour.Mixer.SetTime(0);
			}

			var targetPhase = Phase.Retreating;
			// stop
			if (retreatAbility.ActiveTime >= 1.75f && retreatAbility.ActiveTime < 3.25f)
				targetPhase                                    = Phase.Stop;
			else if (!retreatAbility.IsRetreating) targetPhase = Phase.WalkBack;

			data.Phase = targetPhase;
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultRetreatAbility), typeof(Owner));
		}

		private class Playable__ : PlayableSystem
		{
		}

		protected override ScriptPlayable<PlayableSystem> GetNewPlayable(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			return (ScriptPlayable<PlayableSystem>) (Playable) ScriptPlayable<Playable__>.Create(data.Graph);
		}

		protected override void PlayableOnInitialize(PlayableSystem behavior, ref SystemData systemData)
		{
			foreach (var clip in clipMap)
			{
				var clipPlayable = AnimationClipPlayable.Create(behavior.Graph, clip.Value);
				var idx          = clip.Key.Index;
				if (behavior.Mixer.GetInputCount() <= idx)
					behavior.Mixer.SetInputCount(idx + 1);

				behavior.Mixer.ConnectInput(idx, clipPlayable, 0);
			}
		}

		protected override void PlayablePrepareFrame(PlayableSystem behavior, Playable playable, FrameData info, ref SystemData systemData)
		{
			var global = (float) (behavior.Root.GetTime() - systemData.StartTime);
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
				behavior.Mixer.SetTime(0);
			}

			behavior.Mixer.SetInputWeight((int) Phase.Retreating, 0);
			behavior.Mixer.SetInputWeight((int) Phase.Stop, 0);
			behavior.Mixer.SetInputWeight((int) Phase.WalkBack, 0);
			switch (systemData.Phase)
			{
				case Phase.Retreating:
					behavior.Mixer.SetInputWeight((int) Phase.Retreating, 1);
					break;
				case Phase.Stop:
					behavior.Mixer.SetInputWeight((int) Phase.Retreating, systemData.RetreatingToStopTransition.Evaluate(global));
					behavior.Mixer.SetInputWeight((int) Phase.Stop, systemData.StopFromRetreatingTransition.Evaluate(global, 0, 1));
					break;
				case Phase.WalkBack:
					behavior.Mixer.SetInputWeight((int) Phase.Stop, systemData.StopToWalkTransition.Evaluate(global));
					behavior.Mixer.SetInputWeight((int) Phase.WalkBack, systemData.WalkFromStopTransition.Evaluate(global, 0, 1));
					break;
			}

			var currAnim = behavior.Visual.CurrAnimation;

			systemData.Weight = 0;
			if (currAnim.CanBlend(behavior.Root.GetTime()) && currAnim.PreviousType == SystemType)
				systemData.Weight = currAnim.GetTransitionWeightFixed(behavior.Root.GetTime());
			else if (currAnim.Type == SystemType)
				systemData.Weight = 1;

			behavior.Root.SetInputWeight(VisualAnimation.GetIndexFrom(behavior.Root, behavior.Self), systemData.Weight);
		}
	}
}