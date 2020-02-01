using System.Collections.Generic;
using Patapon.Client.Graphics.Animation.Units;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace package.patapon.core.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class JumpAbilityClientAnimationSystem : BaseAbilityAnimationSystem
	{
		private const string AddrKey = "core://Client/Models/UberHero/Animations/Shared/Jump/Jump{0}.anim";

		private const int             ArrayLength = 4;
		private       AnimationClip[] m_AnimationClips;

		private int m_LoadSuccess;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AnimationClips = new AnimationClip[ArrayLength];
			for (var i = 0; i != ArrayLength; i++)
			{
				var key = "Start";
				switch (i)
				{
					case 0:
						key = "Start";
						break;
					case 1:
						key = "Idle";
						break;
					case 2:
						key = "IdleAir";
						break;
					case 3:
						key = "Fall";
						break;
				}

				LoadAssetAsync<AnimationClip, OperationData>(string.Format(AddrKey, $"{key}"), new OperationData
				{
					ArrayIndex = i
				});
			}
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultJumpAbility), typeof(Owner));
		}

		protected override void OnAsyncOpUpdate(ref int index)
		{
			var (handle, data) = DefaultAsyncOperation.InvokeExecute<AnimationClip, OperationData>(AsyncOp, ref index);
			if (handle.Result == null)
				return;

			m_AnimationClips[data.ArrayIndex] = handle.Result;
			m_LoadSuccess++;
		}

		protected override bool OnBeforeForEach()
		{
			if (!base.OnBeforeForEach())
				return false;
			return m_LoadSuccess >= ArrayLength;
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(playable, data.Index, data.Graph, data.Behavior.RootMixer, m_AnimationClips);

			systemData.ActiveId             = -1;
			systemData.StartAt              = -1;
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

			if (!animation.ContainsSystem(SystemType))
				animation.InsertSystem<SystemData>(SystemType, AddAnimation, RemoveAnimation);

			var abilityState = EntityManager.GetComponentData<AbilityState>(abilityEntity);
			var engineSet = EntityManager.GetComponentData<AbilityEngineSet>(abilityEntity);
			var jumpAbility  = EntityManager.GetComponentData<DefaultJumpAbility>(abilityEntity);
			var velocity     = EntityManager.GetComponentData<Velocity>(targetEntity);

			if (abilityState.Phase == EAbilityPhase.None)
			{
				if (currAnim.Type == SystemType)
					animation.SetTargetAnimation(TargetAnimation.Null);

				return;
			}
			
			var process = EntityManager.GetComponentData<FlowEngineProcess>(EntityManager.GetComponentData<Relative<RhythmEngineDescription>>(backend.DstEntity).Target);

			ref var data = ref animation.GetSystemData<SystemData>(SystemType);
			if ((abilityState.Phase & EAbilityPhase.WillBeActive) != 0 && data.StartAt < 0 && abilityState.UpdateVersion >= data.ActiveId)
			{
				var delay   = math.max(engineSet.CommandState.StartTime - 200 - process.Milliseconds, 0) * 0.001f;
				// StartTime - StartJump Animation Approx Length in ms - Time, aka delay 0.2s before the command

				//Debug.Log($"[{Time.ElapsedTime}] {engineSet.CommandState.StartTime - process.Milliseconds} {delay} activeId:{abilityState.UpdateVersion + 1} startAt {animation.RootTime + delay}");

				data.StartAt             = animation.RootTime + delay;
				data.ActiveId            = abilityState.UpdateVersion + 1;
				data.Behaviour.Predicted = true;
			}

			// Start animation if Behavior.ActiveId and Jump.ActiveId is different... or if we need to start now
			if ((abilityState.Phase & EAbilityPhase.Active) != 0 && abilityState.UpdateVersion > data.ActiveId
			    || data.StartAt > 0 && data.StartAt < animation.RootTime)
			{
				var stopAt = animation.RootTime + (engineSet.CommandState.ChainEndTime - process.Milliseconds) * 0.001f - 0.75f;
				animation.SetTargetAnimation(new TargetAnimation(SystemType, false, false, stopAt: stopAt));
				//Debug.Log($"[{Time.ElapsedTime}] Start Animation [{abilityState.UpdateVersion} > {data.ActiveId}] || [0 < {data.StartAt} < {data.Behaviour.Root.GetTime()}]");

				data.Behaviour.Mixer.SetTime(0);
				data.Behaviour.StartTime = animation.RootTime;

				data.StartAt = -1;
				if (abilityState.Phase == EAbilityPhase.Active)
					data.ActiveId = abilityState.UpdateVersion;

				data.Behaviour.Weight = 1;
			}

			var targetPhase = Phase.Idle;
			if (jumpAbility.IsJumping || abilityState.Phase == EAbilityPhase.WillBeActive)
				targetPhase = Phase.Jumping;
			else if (velocity.Value.y < 0)
				targetPhase = Phase.Fall;

			data.Behaviour.Phase = targetPhase;
		}

		private enum Phase
		{
			Jumping,
			Idle,
			Fall
		}

		private class SystemPlayable : PlayableBehaviour
		{
			public Transition FallTransition1;
			public Transition FallTransition2;

			public Transition IdleAirTransition1;
			public Transition IdleAirTransition2;
			public Transition JumpTransition;

			private Phase m_PreviousPhase;

			public AnimationMixerPlayable Mixer;
			public Phase                  Phase;

			public bool                   Predicted;
			public AnimationMixerPlayable Root;

			public Playable Self;

			public double StartTime;

			public Transition                      StartTransition;
			public UnitVisualPlayableBehaviourData VisualData;

			public float Weight;

			public void Initialize(Playable self, int index, PlayableGraph graph, AnimationMixerPlayable rootMixer, IReadOnlyList<AnimationClip> clips)
			{
				Self = self;
				Root = rootMixer;

				Mixer = AnimationMixerPlayable.Create(graph, clips.Count, true);
				Mixer.SetPropagateSetTime(true);
				for (var i = 0; i != clips.Count; i++)
				{
					var clipPlayable = AnimationClipPlayable.Create(graph, clips[i]);

					Mixer.ConnectInput(i, clipPlayable, 0);
				}

				rootMixer.AddInput(self, 0, 1);
				self.AddInput(Mixer, 0, 1);

				StartTransition = new Transition(clips[0], 0.75f, 1f);
				JumpTransition  = new Transition(StartTransition, 0.4f, 0.5f);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global = (float) (Root.GetTime() - StartTime);

				if (m_PreviousPhase != Phase)
				{
					switch (m_PreviousPhase)
					{
						case Phase.Jumping when Phase == Phase.Idle:
							IdleAirTransition1.End(global, global + 0.1f);
							IdleAirTransition2.Begin(global, global + 0.1f);
							IdleAirTransition2.End(global + 0.1f, global + 0.1f);
							break;
						case Phase.Idle when Phase == Phase.Fall:
							FallTransition1.End(global, global + 0.1f);
							FallTransition2.Begin(global, global + 0.1f);
							FallTransition2.End(global, global + 0.1f);
							break;
					}

					m_PreviousPhase = Phase;
				}

				Mixer.SetInputWeight((int) AnimationType.Start, 0);
				Mixer.SetInputWeight((int) AnimationType.Jump, 0);
				Mixer.SetInputWeight((int) AnimationType.IdleAir, 0);
				Mixer.SetInputWeight((int) AnimationType.Fall, 0);
				switch (Phase)
				{
					case Phase.Jumping:
						if (Predicted)
						{
							Mixer.SetInputWeight((int) AnimationType.Start, StartTransition.Evaluate(global));
							Mixer.SetInputWeight((int) AnimationType.Jump, JumpTransition.Evaluate(global, 0, 1));
						}
						else
						{
							Mixer.SetInputWeight((int) AnimationType.Start, 0);
							Mixer.SetInputWeight((int) AnimationType.Jump, 1);
						}

						break;
					case Phase.Idle:
						Mixer.SetInputWeight((int) AnimationType.Jump, IdleAirTransition1.Evaluate(global));
						Mixer.SetInputWeight((int) AnimationType.IdleAir, IdleAirTransition2.Evaluate(global, 0, 1));
						break;
					case Phase.Fall:
						Mixer.SetInputWeight((int) AnimationType.IdleAir, FallTransition1.Evaluate(global));
						Mixer.SetInputWeight((int) AnimationType.Fall, FallTransition2.Evaluate(global, 0, 1));
						break;
				}

				var currAnim   = VisualData.CurrAnimation;
				var systemType = typeof(JumpAbilityClientAnimationSystem);

				Weight = 0;
				if (currAnim.CanBlend(Root.GetTime()) && currAnim.PreviousType == systemType)
					Weight                                   = currAnim.GetTransitionWeightFixed(Root.GetTime());
				else if (currAnim.Type == systemType) Weight = 1;

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}

			private enum AnimationType
			{
				Start   = 0,
				Jump    = 1,
				IdleAir = 2,
				Fall    = 3
			}
		}

		private struct SystemData
		{
			public SystemPlayable Behaviour;

			public int    ActiveId;
			public double StartAt;
		}

		private struct OperationData
		{
			public int ArrayIndex;
		}
	}
}