using System;
using System.Threading.Tasks;
using GameHost.ShareSimuWorldFeature.Systems;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using PataNext.Module.Simulation.Components;
using PataNext.Module.Simulation.Components.GamePlay.RhythmEngine;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Game.RhythmEngine;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class UnitDrumReactionAnimation : BaseAnimationSystem, IAbilityPlayableSystemCalls
	{
		protected override AnimationMap GetAnimationMap()
		{
			return new AnimationMap<int>("DrumReaction/Animations/")
			{
				{"Pata", 0},
				{"Pon", 1},
				{"Don", 2},
				{"Chaka", 3},
			};
		}

		private InterFrame interFrame;

		protected override bool OnBeforeForEach()
		{
			if (HasSingleton<InterFrame>())
				interFrame = GetSingleton<InterFrame>();

			return base.OnBeforeForEach();
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

			if (!animation.CurrAnimation.AllowOverride)
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
				if (interFrame.Range.Contains(rhythmActions[i].InterFrame.Pressed))
					pressureKey = i;
			}

			InjectAnimationWithSystemData<SystemData>();

			ref var data = ref animation.GetSystemData<SystemData>(SystemType);
			// If either there is no input, or that the clip with this drum isn't yet loaded: stop here.
			if (pressureKey < 0 || data.LoadedClips[pressureKey] == null)
				return;

			pressureKey++; // Increase by one since drums start at 1

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

			var transitionStart = data.LoadedClips[pressureKey - 1].length * 0.825f + animation.RootTime;
			var transitionEnd   = data.LoadedClips[pressureKey - 1].length + animation.RootTime;

			animation.SetTargetAnimation(new TargetAnimation(SystemType, allowTransition: true, transitionStart: transitionStart, transitionEnd: transitionEnd));
			data.CurrentKey      = pressureKey;
			data.TransitionStart = transitionStart;
			data.TransitionEnd   = transitionEnd;
			data.bv.Mixer.SetTime(0);
			data.bv.Self.Play();
		}

		protected override IAbilityPlayableSystemCalls GetPlayableCalls() => this;

		private struct SystemData
		{
			public int  CurrentKey;

			public AnimationClip[] LoadedClips;
			public double          TransitionStart;
			public double          TransitionEnd;
			
			public PlayableInnerCall bv;
		}

		void IAbilityPlayableSystemCalls.OnInitialize(PlayableInnerCall behavior)
		{
			ref var systemData = ref behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);
			
			systemData.LoadedClips = new AnimationClip[4];
			systemData.bv        = behavior;
			
			behavior.Mixer.SetInputCount(4);
			foreach (var kvp in ((AnimationMap<int>) AnimationMap).KeyDataMap)
			{
				behavior.AddAsyncOp(AnimationMap.Resolve(kvp.Key, GetCurrentClipProvider()), handle =>
				{
					var cp = AnimationClipPlayable.Create(behavior.Graph, ((Task<AnimationClip>) handle).Result);
					if (cp.IsNull())
						throw new InvalidOperationException("null clip");

					behavior.Graph.Connect(cp, 0, behavior.Mixer, kvp.Value);
					behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType).LoadedClips[kvp.Value] = cp.GetAnimationClip();
				});
			}
		}

		void IAbilityPlayableSystemCalls.PrepareFrame(PlayableInnerCall behavior, Playable playable, FrameData info)
		{
			ref readonly var data = ref behavior.Visual.VisualAnimation.GetSystemData<SystemData>(SystemType);

			var inputCount = behavior.Mixer.GetInputCount();
			var weight     = VisualAnimation.GetWeightFixed(behavior.Root.GetTime(), data.TransitionStart, data.TransitionEnd);

			for (var i = 0; i != inputCount; i++) behavior.Mixer.SetInputWeight(i, i == data.CurrentKey - 1 ? 1 : 0);

			if ((!behavior.Visual.CurrAnimation.AllowTransition || behavior.Visual.CurrAnimation.PreviousType != SystemType)
			    && behavior.Visual.CurrAnimation.Type != SystemType) weight = 0;

			behavior.Root.SetInputWeight(VisualAnimation.GetIndexFrom(behavior.Root, behavior.Self), weight);
		}
	}
}