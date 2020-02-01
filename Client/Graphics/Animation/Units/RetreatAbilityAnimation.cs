using System.Collections.Generic;
using Patapon.Client.Graphics.Animation.Units;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace package.patapon.core.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class RetreatAbilityClientAnimationSystem : BaseAbilityAnimationSystem
	{
		private const string AddrPath       = "core://Client/Models/UberHero/Animations/Shared/";
		private const string AddrRetreatKey = AddrPath + "Retreat/Retreat{0}.anim";
		private const string AddrMarchKey   = AddrPath + "Walking.anim";

		private const int             ArrayLength = 3;
		private       AnimationClip[] m_AnimationClips;

		private int m_LoadSuccess;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AnimationClips = new AnimationClip[ArrayLength];
			for (var i = 0; i != ArrayLength - 1; i++)
			{
				var key = "Run";
				switch (i)
				{
					case 0:
						key = "Run";
						break;
					case 1:
						key = "Stop";
						break;
				}

				LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrRetreatKey, $"{key}"), new OperationHandleData {ArrayIndex = i});
			}

			LoadAssetAsync<AnimationClip, OperationHandleData>(AddrMarchKey, new OperationHandleData {ArrayIndex = ArrayLength - 1});
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultRetreatAbility), typeof(Owner));
		}

		protected override void OnAsyncOpUpdate(ref int index)
		{
			var (handle, data) = DefaultAsyncOperation.InvokeExecute<AnimationClip, OperationHandleData>(AsyncOp, ref index);
			if (handle.Result == null)
				return;

			m_AnimationClips[data.ArrayIndex] = handle.Result;
			m_LoadSuccess++;
		}

		protected override bool OnBeforeForEach()
		{
			if (!base.OnBeforeForEach())
				return false;

			return m_LoadSuccess >= m_AnimationClips.Length;
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(this, data.Graph, playable, data.Index, data.Behavior.RootMixer, new PlayableInitData {Clips = m_AnimationClips});

			systemData.ActiveId             = -1;
			systemData.Behaviour            = behavior;
			systemData.Behaviour.VisualData = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		private void RemoveAnimation(VisualAnimation.ManageData data, SystemData systemData)
		{
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type == SystemType && animation.RootTime > currAnim.StopAt)
			{
				// allow transitions and overrides now...
				animation.SetTargetAnimation(new TargetAnimation(currAnim.Type, transitionStart: currAnim.StopAt, transitionEnd: currAnim.StopAt + 0.25f));
				// if no one set another animation, then let's set to null...
				if (animation.RootTime > currAnim.StopAt + 0.25f)
					animation.SetTargetAnimation(TargetAnimation.Null);
			}

			var abilityEntity = AbilityFinder.GetAbility(targetEntity);
			if (abilityEntity == default)
				return;

			if (!animation.ContainsSystem(SystemType)) animation.InsertSystem<SystemData>(SystemType, AddAnimation, RemoveAnimation);

			var abilityState   = EntityManager.GetComponentData<AbilityState>(abilityEntity);
			var retreatAbility = EntityManager.GetComponentData<DefaultRetreatAbility>(abilityEntity);

			if (abilityState.Phase == EAbilityPhase.None)
			{
				if (currAnim.Type == SystemType)
					animation.SetTargetAnimation(TargetAnimation.Null);

				return;
			}

			ref var data = ref animation.GetSystemData<SystemData>(SystemType);
			
			// Start animation if Behavior.ActiveId and Retreat.ActiveId is different
			if ((abilityState.Phase & EAbilityPhase.ActiveOrChaining) != 0 && abilityState.ActivationVersion != data.ActiveId)
			{
				var stopAt = animation.RootTime + 3.25f;
				animation.SetTargetAnimation(new TargetAnimation(SystemType, false, false, stopAt: stopAt));

				data.ActiveId            = abilityState.ActivationVersion;
				data.Behaviour.StartTime = animation.RootTime;
				data.Behaviour.Mixer.SetTime(0);
				data.Behaviour.Weight = 1;
			}

			var targetPhase = Phase.Retreating;
			// stop
			if (retreatAbility.ActiveTime >= 1.75f && retreatAbility.ActiveTime <= 3.25f)
				targetPhase                                    = Phase.Stop;
			else if (!retreatAbility.IsRetreating) targetPhase = Phase.WalkBack;

			data.Behaviour.Phase = targetPhase;
		}

		private enum Phase
		{
			Retreating,
			Stop,
			WalkBack
		}

		public struct PlayableInitData
		{
			public IReadOnlyList<AnimationClip> Clips;
		}

		private class SystemPlayable : BasePlayable<PlayableInitData>
		{
			private Phase m_PreviousPhase;
			public  Phase Phase;

			public Transition RetreatingToStopTransition;

			public double     StartTime;
			public Transition StopFromRetreatingTransition;

			public Transition StopToWalkTransition;

			public UnitVisualPlayableBehaviourData VisualData;
			public Transition                      WalkFromStopTransition;

			public float Weight;

			protected override void OnInitialize(PlayableInitData init)
			{
				for (var i = 0; i != init.Clips.Count; i++)
				{
					var clipPlayable = AnimationClipPlayable.Create(Graph, init.Clips[i]);

					Mixer.ConnectInput(i, clipPlayable, 0);
				}
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = (float) (Root.GetTime() - StartTime);
				if (m_PreviousPhase != Phase)
				{
					switch (m_PreviousPhase)
					{
						case Phase.Retreating when Phase == Phase.Stop:
							RetreatingToStopTransition.End(global, global + 0.15f);
							StopFromRetreatingTransition.Begin(global, global + 0.15f);
							StopFromRetreatingTransition.End(global, global + 0.15f);
							break;
						case Phase.Stop when Phase == Phase.WalkBack:
							StopToWalkTransition.End(global, global + 0.33f);
							WalkFromStopTransition.Begin(global, global + 0.33f);
							WalkFromStopTransition.End(global, global + 0.33f);
							break;
					}

					m_PreviousPhase = Phase;
					Mixer.SetTime(0);
				}

				Mixer.SetInputWeight((int) AnimationType.Retreat, 0);
				Mixer.SetInputWeight((int) AnimationType.Stop, 0);
				Mixer.SetInputWeight((int) AnimationType.WalkBack, 0);
				switch (Phase)
				{
					case Phase.Retreating:
						Mixer.SetInputWeight((int) AnimationType.Retreat, 1);
						break;
					case Phase.Stop:
						Mixer.SetInputWeight((int) AnimationType.Retreat, RetreatingToStopTransition.Evaluate(global));
						Mixer.SetInputWeight((int) AnimationType.Stop, StopFromRetreatingTransition.Evaluate(global, 0, 1));
						break;
					case Phase.WalkBack:
						Mixer.SetInputWeight((int) AnimationType.Stop, StopToWalkTransition.Evaluate(global));
						Mixer.SetInputWeight((int) AnimationType.WalkBack, WalkFromStopTransition.Evaluate(global, 0, 1));
						break;
				}

				var currAnim = VisualData.CurrAnimation;

				Weight = 0;
				if (currAnim.CanBlend(Root.GetTime()) && currAnim.PreviousType == SystemType)
					Weight                                   = currAnim.GetTransitionWeightFixed(Root.GetTime());
				else if (currAnim.Type == SystemType) Weight = 1;

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}

			private enum AnimationType
			{
				Retreat  = 0,
				Stop     = 1,
				WalkBack = 2 // walk back will be the normal walk animation
			}
		}

		private struct SystemData
		{
			public SystemPlayable Behaviour;

			public int ActiveId;
		}

		private struct OperationHandleData
		{
			public int ArrayIndex;
		}
	}
}