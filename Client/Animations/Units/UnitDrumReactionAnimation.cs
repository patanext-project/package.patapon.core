using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components;
using PataNext.Module.Simulation.Components.GamePlay.Abilities;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.GamePlay.Units;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Game.RhythmEngine;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.Systems;
using StormiumTeam.GameBase.Utility.Misc;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class UnitDrumReactionAnimation : BaseAnimationSystem, IAbilityPlayableSystemCalls
	{
		public enum EType
		{
			Normal,
			Ferocious
		}
		
		protected override AnimationMap GetAnimationMap()
		{
			return new AnimationMap<(int index, EType type)>("DrumReaction/Animations/")
			{
				{"Pata", (1, EType.Normal)},
				{"Pon", (2, EType.Normal)},
				{"Don", (3, EType.Normal)},
				{"Chaka", (4, EType.Normal)},
				
				{"PataFerocious", (1, EType.Ferocious)},
				{"PonFerocious", (2, EType.Ferocious)},
				{"DonFerocious", (3, EType.Ferocious)},
				{"ChakaFerocious", (4, EType.Ferocious)},
			};
		}

		private TimeSystem timeSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			timeSystem = World.GetExistingSystem<TimeSystem>();
		}

		private void UpdatePersistent(UnitVisualAnimation animation)
		{
			if (!animation.ContainsSystem(SystemType))
				return;

			var currAnim = animation.CurrAnimation;

			ref var data = ref animation.GetSystemData<SystemData>(SystemType);

			if (currAnim.Type != SystemType && !currAnim.CanBlend(animation.RootTime)) data.TransitionEnd = -1;
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			UpdatePersistent(animation);

			if (!animation.CurrAnimation.AllowOverride
			    || (EntityManager.TryGetComponentData(targetEntity, out OwnerActiveAbility activeAbility)
			        && activeAbility.Active != default
			        && (EntityManager.GetComponentData<AbilityActivation>(activeAbility.Active).Type & EAbilityActivationType.HeroMode) != 0))
				return;

			if (!EntityManager.TryGetComponentData(targetEntity, out Relative<PlayerDescription> relativePlayer)
			    || !EntityManager.TryGetComponentData(relativePlayer.Target, out GameRhythmInputComponent playerCommand))
				return;

			if (!EntityManager.TryGetComponentData(targetEntity, out Relative<RhythmEngineDescription> relativeEngine))
				return;

			var process  = EntityManager.GetComponentData<RhythmEngineLocalState>(relativeEngine.Target);
			var settings = EntityManager.GetComponentData<RhythmEngineSettings>(relativeEngine.Target);

			if (!EntityManager.HasComponent<RhythmEngineIsPlaying>(relativeEngine.Target)
			    || RhythmEngineUtility.GetFlowBeat(process.Elapsed, settings.BeatInterval) < 0)
				return;

			var pressureKey   = -1;
			var rhythmActions = playerCommand.Actions;
			for (var i = 0; pressureKey < 0 && i != rhythmActions.Length; i++)
			{
				if (timeSystem.GetReport(relativePlayer.Target).Active.Contains(rhythmActions[i].InterFrame.Pressed))
					pressureKey = i;
			}

			InjectAnimationWithSystemData<SystemData>();

			ref var data = ref animation.GetSystemData<SystemData>(SystemType);
			// If either there is no input, or that the clip with this drum isn't yet loaded: stop here.
			// If we queued a pressure, then virtually modify the pressure key
			if (pressureKey != -1)
				data.Queue = -1;
			
			if (data.Queue > 0)
			{
				data.Queue  = -1;
				pressureKey = data.CurrentKey - 1;
			}

			pressureKey++; // Increase by one since drums start at 1
			if (pressureKey <= 0 || data.ClipPlayableMap[pressureKey].Count == 0)
				return;
			
			ResetIdleTime(targetEntity);

			// invert keys...
			if (EntityManager.TryGetComponentData(targetEntity, out UnitDirection unitDirection)
			    && unitDirection.IsLeft)
			{
				pressureKey = pressureKey switch
				{
					RhythmKeys.Pata => RhythmKeys.Pon,
					RhythmKeys.Pon => RhythmKeys.Pata,
					_ => pressureKey
				};
			}

			var wantedType = EType.Normal;
			if (EntityManager.TryGetComponentData(targetEntity, out UnitEnemySeekingState seekingState)
			    && seekingState.Enemy != default)
				wantedType = EType.Ferocious;

			if (false == data.ClipPlayableMap[pressureKey].ContainsKey(wantedType))
			{
				foreach (var key in data.ClipPlayableMap[pressureKey].Keys)
				{
					wantedType = key;
					break;
				}
			}

			var clip = data.ClipPlayableMap[pressureKey][wantedType]
			               .GetAnimationClip();

			if (clip == null)
				return;

			var rand = 0f;
			if (animation.Presentation.TryGetComponent(out OverrideObjectComponent overrides)
			    && overrides.TryGetFloat("ReactionTime_Max", out var randMax))
			{
				if (UnityEngine.Random.Range(0, 10) > 8)
					randMax *= 0.8f;
				if (UnityEngine.Random.Range(0, 10) > 6)
					randMax *= 0.6f;
				if (UnityEngine.Random.Range(0, 10) > 4)
					randMax *= 0.4f;
				if (UnityEngine.Random.Range(0, 10) > 2)
					randMax *= 0.2f;
				
				rand = UnityEngine.Random.Range(0, randMax);
			}

			if (rand > Time.DeltaTime)
			{
				data.CurrentKey = pressureKey;
				data.Queue      = Time.ElapsedTime + rand;
				return;
			}

			var transitionStart = clip.length * 0.825f + animation.RootTime;
			var transitionEnd   = clip.length + animation.RootTime;

			animation.SetTargetAnimation(new TargetAnimation(SystemType, allowTransition: true, transitionStart: transitionStart, transitionEnd: transitionEnd));
			data.CurrentKey      = pressureKey;
			data.TransitionStart = transitionStart;
			data.TransitionEnd   = transitionEnd;
			data.MixerMap[pressureKey].SetPropagateSetTime(true);
			data.MixerMap[pressureKey].SetTime(0);
			data.bv.Self.Play();
		}

		protected override IAbilityPlayableSystemCalls GetPlayableCalls() => this;

		private struct SystemData
		{
			public int   CurrentKey;
			public EType CurrentType;

			public Dictionary<int, Dictionary<EType, AnimationClipPlayable>> ClipPlayableMap;
			public Dictionary<int, AnimationMixerPlayable>                   MixerMap;

			public double TransitionStart;
			public double TransitionEnd;

			public PlayableInnerCall bv;

			public double Queue;
		}


		void IAbilityPlayableSystemCalls.OnInitialize(PlayableInnerCall behavior)
		{
			var systemData = behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);
			systemData.ClipPlayableMap = new Dictionary<int, Dictionary<EType, AnimationClipPlayable>>
			{
				{1, new Dictionary<EType, AnimationClipPlayable>()},
				{2, new Dictionary<EType, AnimationClipPlayable>()},
				{3, new Dictionary<EType, AnimationClipPlayable>()},
				{4, new Dictionary<EType, AnimationClipPlayable>()},
			};
			systemData.MixerMap = new Dictionary<int, AnimationMixerPlayable>
			{
				{1, AnimationMixerPlayable.Create(behavior.Graph, 2, true)},
				{2, AnimationMixerPlayable.Create(behavior.Graph, 2, true)},
				{3, AnimationMixerPlayable.Create(behavior.Graph, 2, true)},
				{4, AnimationMixerPlayable.Create(behavior.Graph, 2, true)},
			};
			systemData.bv = behavior;
			
			behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType) = systemData;

			foreach (var kvp in ((AnimationMap<(int target, EType type)>) AnimationMap).KeyDataMap)
			{
				behavior.AddAsyncOp(AnimationMap.Resolve(kvp.Key, GetCurrentClipProvider()), handle =>
				{
					if (((Task<AnimationClip>) handle).Result == null)
					{
						Debug.LogError($"null clip {kvp.Value.target}, {kvp.Value.type}");
						return;
					}
					
					var cp = AnimationClipPlayable.Create(behavior.Graph, ((Task<AnimationClip>) handle).Result);
					if (cp.IsNull())
					{
						throw new InvalidOperationException($"null clip {kvp.Value.target}, {kvp.Value.type}");
					}

					behavior.Graph.Connect(cp, 0, systemData.MixerMap[kvp.Value.target], (int) kvp.Value.type);
					systemData.ClipPlayableMap[kvp.Value.target][kvp.Value.type] = cp;
				});
			}

			for (var i = 1; i <= 4; i++)
			{
				behavior.Mixer.AddInput(systemData.MixerMap[i], 0, 1);
			}
		}

		void IAbilityPlayableSystemCalls.PrepareFrame(PlayableInnerCall behavior, Playable playable, FrameData info)
		{
			ref readonly var data = ref behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);

			var inputCount = behavior.Mixer.GetInputCount();
			var weight     = VisualAnimation.GetWeightFixed(behavior.Root.GetTime(), data.TransitionStart, data.TransitionEnd);

			for (var i = 0; i != inputCount; i++) behavior.Mixer.SetInputWeight(i, i + 1 == data.CurrentKey ? 1 : 0);

			foreach (var kvp in data.MixerMap)
			{
				var isNormal = data.CurrentType == EType.Normal || !data.ClipPlayableMap[kvp.Key].ContainsKey(EType.Ferocious);
				kvp.Value.SetInputWeight((int) EType.Normal, isNormal ? 1 : 0);
				kvp.Value.SetInputWeight((int) EType.Ferocious, !isNormal ? 1 : 0);
			}

			if ((!behavior.Visual.CurrAnimation.AllowTransition || behavior.Visual.CurrAnimation.PreviousType != SystemType)
			    && behavior.Visual.CurrAnimation.Type != SystemType) weight = 0;

			behavior.Root.SetInputWeight(VisualAnimation.GetIndexFrom(behavior.Root, behavior.Self), weight);
		}
	}
}