using System.Collections.Generic;
using Patapon.Client.Graphics.Animation.Units;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities;
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
	public class MarchAbilityClientAnimationSystem : BaseAbilityAnimationSystem
	{
		private const string AddrKey = "core://Client/Models/UberHero/Animations/Shared/{0}.anim";

		private AnimationClip[] m_Clips;
		private int             m_LoadSuccess;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Clips = new AnimationClip[2];

			LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrKey, "Idle"), new OperationHandleData {Index    = 0});
			LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrKey, "Walking"), new OperationHandleData {Index = 1});
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(DefaultMarchAbility), typeof(Owner));
		}

		protected override void OnAsyncOpUpdate(ref int index)
		{
			var (handle, data) = AsyncOp.Get<AnimationClip, OperationHandleData>(index);
			if (handle.Result == null)
				return;

			m_Clips[data.Index] = handle.Result;
			m_LoadSuccess++;

			AsyncOp.Handles.RemoveAtSwapBack(index);
			index--;
		}

		protected override bool OnBeforeForEach()
		{
			if (!base.OnBeforeForEach())
				return false;

			return m_LoadSuccess >= m_Clips.Length;
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(playable, data.Index, data.Graph, data.Behavior.RootMixer, m_Clips);

			systemData.Playable             = playable;
			systemData.Behaviour            = behavior;
			systemData.Behaviour.VisualData = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		private void RemoveAnimation(VisualAnimation.ManageData manageData, SystemData systemData)
		{
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type != SystemType && !currAnim.AllowOverride)
			{
				if (animation.ContainsSystem(SystemType)) animation.GetSystemData<SystemData>(SystemType).Behaviour.Weight = 0;

				return;
			}

			var abilityEntity = AbilityFinder.GetAbility(backend.DstEntity);
			if (abilityEntity == default && currAnim.Type == SystemType || currAnim.CanStartAnimationAt(animation.RootTime)) animation.SetTargetAnimation(new TargetAnimation(null, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd, previousType: currAnim.Type));

			if (!animation.ContainsSystem(SystemType)) animation.InsertSystem<SystemData>(SystemType, AddAnimation, RemoveAnimation);

			var systemData  = animation.GetSystemData<SystemData>(SystemType);
			var doAnimation = currAnim == TargetAnimation.Null || currAnim.Type == SystemType;

			var abilityActive = false;
			if (abilityEntity != default)
			{
				var abilityState             = EntityManager.GetComponentData<RhythmAbilityState>(abilityEntity);
				doAnimation |= abilityActive = abilityState.IsActive;
			}

			if (abilityActive) systemData.Behaviour.ForceAnimation = true;

			var velocity = EntityManager.GetComponentData<Velocity>(backend.DstEntity);
			systemData.Behaviour.TargetAnimation = math.abs(velocity.Value.x) > 0f || abilityActive ? 1 : 0;

			if (!doAnimation)
				return;

			animation.SetTargetAnimation(new TargetAnimation(SystemType, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd, previousType: currAnim.PreviousType));
		}

		private class SystemPlayable : PlayableBehaviour
		{
			public  bool       ForceAnimation; // no transition
			public  Transition FromTransition;
			private int        m_PreviousAnimation;

			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public Playable Self;
			public int      TargetAnimation;

			public Transition                      ToTransition;
			public UnitVisualPlayableBehaviourData VisualData;
			public float                           Weight;

			public void Initialize(Playable self, int index, PlayableGraph graph, AnimationMixerPlayable rootMixer, IReadOnlyList<AnimationClip> clips)
			{
				Self = self;
				Root = rootMixer;

				Mixer = AnimationMixerPlayable.Create(graph, clips.Count, true);
				Mixer.SetPropagateSetTime(true);

				for (var i = 0; i != clips.Count; i++)
				{
					var clipPlayable = AnimationClipPlayable.Create(graph, clips[i]);

					graph.Connect(clipPlayable, 0, Mixer, i);
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
					if (m_PreviousAnimation == 1) // walking
					{
						var clipPlayable = (AnimationClipPlayable) Mixer.GetInput(m_PreviousAnimation);
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

				var inputCount = Mixer.GetInputCount();
				for (var i = 0; i != inputCount; i++) Mixer.SetInputWeight(i, i == TargetAnimation ? FromTransition.Evaluate(global, 0, 1) : ToTransition.Evaluate(global));

				Weight = 1 - VisualData.CurrAnimation.GetTransitionWeightFixed(VisualData.VisualAnimation.RootTime);
				if (VisualData.CurrAnimation.Type != typeof(MarchAbilityClientAnimationSystem) && !VisualData.CurrAnimation.CanBlend(VisualData.RootTime)) Weight = 0;

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}
		}

		private struct SystemData
		{
			public ScriptPlayable<SystemPlayable> Playable;
			public SystemPlayable                 Behaviour;
		}

		private struct OperationHandleData
		{
			public int Index;
		}
	}
}