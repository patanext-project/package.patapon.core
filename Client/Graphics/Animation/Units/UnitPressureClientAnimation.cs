using package.stormiumteam.shared.ecs;
using Patapon.Client.Graphics.Animation.Units;
using Patapon.Mixed;
using Patapon.Mixed.RhythmEngine;
using Patapon.Mixed.RhythmEngine.Flow;
using Patapon.Mixed.Units;
using Patapon4TLB.Default.Player;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace package.patapon.core.Animation.Units
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class UnitPressureClientAnimation : BaseAnimationSystem
	{
		private const string AddrKey = "core://Client/Models/UberHero/Animations/Shared/{0}.anim";

		private AnimationClip[] m_AnimationClips = new AnimationClip[0];
		private int             m_LoadSuccess;

		private EntityQuery m_PressureEventQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_AnimationClips = new AnimationClip[4];
			LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrKey, "Pata"), new OperationHandleData {Index  = 0});
			LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrKey, "Pon"), new OperationHandleData {Index   = 1});
			LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrKey, "Don"), new OperationHandleData {Index   = 2});
			LoadAssetAsync<AnimationClip, OperationHandleData>(string.Format(AddrKey, "Chaka"), new OperationHandleData {Index = 3});
		}

		protected override void OnAsyncOpUpdate(ref int index)
		{
			var (handle, data) = DefaultAsyncOperation.InvokeExecute<AnimationClip, OperationHandleData>(AsyncOp, ref index);
			if (handle.Result == null)
				return;

			m_AnimationClips[data.Index] = handle.Result;
			m_LoadSuccess++;
		}

		protected override bool OnBeforeForEach()
		{
			if (!base.OnBeforeForEach())
				return false;

			return m_LoadSuccess >= m_AnimationClips.Length;
		}

		private void UpdatePersistent(UnitVisualAnimation animation)
		{
			if (!animation.ContainsSystem(SystemType))
				return;

			var currAnim = animation.CurrAnimation;

			ref var data                                                                                            = ref animation.GetSystemData<SystemData>(SystemType);
			if (currAnim.Type != SystemType && !currAnim.CanBlend(animation.RootTime)) data.Behaviour.TransitionEnd = -1;
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			UpdatePersistent(animation);

			if (!animation.CurrAnimation.AllowOverride)
				return;

			if (!EntityManager.TryGetComponentData(targetEntity, out Relative<PlayerDescription> relativePlayer)
			    || !EntityManager.TryGetComponentData(relativePlayer.Target, out GamePlayerCommand playerCommand))
				return;

			if (!EntityManager.TryGetComponentData(targetEntity, out Relative<RhythmEngineDescription> relativeEngine))
				return;
			
			var process  = EntityManager.GetComponentData<FlowEngineProcess>(relativeEngine.Target);
			var state    = EntityManager.GetComponentData<RhythmEngineState>(relativeEngine.Target);

			if (state.IsPaused || process.Milliseconds <= 0)
				return;
				
			var pressureKey   = -1;
			var rhythmActions = playerCommand.Base.GetRhythmActions();
			for (var i = 0; pressureKey < 0 && i != rhythmActions.Length; i++)
				if (rhythmActions[i].WasPressed)
					pressureKey = i;

			if (pressureKey < 0)
				return;

			if (EntityManager.TryGetComponentData(targetEntity, out AnimationIdleTime idleTime))
			{
				EntityManager.SetComponentData(targetEntity, default(AnimationIdleTime));
			}

			pressureKey++;

			// invert keys...
			if (EntityManager.TryGetComponentData(targetEntity, out UnitDirection unitDirection)
			    && unitDirection.IsLeft)
			{
				if (pressureKey == RhythmKeys.Left) pressureKey       = RhythmKeys.Right;
				else if (pressureKey == RhythmKeys.Right) pressureKey = RhythmKeys.Left;
			}

			if (!animation.ContainsSystem(SystemType)) animation.InsertSystem<SystemData>(SystemType, AddAnimationData, RemoveAnimationData);

			ref var data = ref animation.GetSystemData<SystemData>(SystemType);

			var transitionStart = m_AnimationClips[pressureKey - 1].length * 0.825f + animation.RootTime;
			var transitionEnd   = m_AnimationClips[pressureKey - 1].length + animation.RootTime;

			animation.SetTargetAnimation(new TargetAnimation(SystemType, allowTransition: true, transitionStart: transitionStart, transitionEnd: transitionEnd));
			data.CurrentKey                = pressureKey;
			data.Behaviour.TransitionStart = transitionStart;
			data.Behaviour.TransitionEnd   = transitionEnd;

			data.Mixer.SetTime(0);
			data.Playable.Play();
		}

		private void AddAnimationData(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();

			behavior.Initialize(this, data.Graph, playable, data.Index, data.Behavior.RootMixer, new PlayableInitData {Clips = m_AnimationClips});

			systemData.Playable             = playable;
			systemData.Behaviour            = behavior;
			systemData.Behaviour.VisualData = ((UnitVisualAnimation) data.Handle).GetBehaviorData();
		}

		private void RemoveAnimationData(VisualAnimation.ManageData data, SystemData systemData)
		{
			systemData.Mixer.Destroy();
		}

		public struct PlayableInitData
		{
			public AnimationClip[] Clips;
		}

		public class SystemPlayable : BasePlayable<PlayableInitData>
		{
			public int    CurrentKey;
			public double TransitionEnd;

			public double                          TransitionStart;
			public UnitVisualPlayableBehaviourData VisualData;

			protected override void OnInitialize(PlayableInitData data)
			{
				for (var i = 0; i != data.Clips.Length; i++)
				{
					var clipPlayable = AnimationClipPlayable.Create(Graph, data.Clips[i]);
					Graph.Connect(clipPlayable, 0, Mixer, i);
				}
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var inputCount = Mixer.GetInputCount();
				var e          = VisualAnimation.GetWeightFixed(Root.GetTime(), TransitionStart, TransitionEnd);

				for (var i = 0; i != inputCount; i++) Mixer.SetInputWeight(i, i == CurrentKey - 1 ? 1 : 0);

				if (!VisualData.CurrAnimation.AllowTransition && VisualData.CurrAnimation.Type != SystemType) e = 0;

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), e);
			}
		}

		private struct SystemData
		{
			public ScriptPlayable<SystemPlayable> Playable;
			public SystemPlayable                 Behaviour;

			public AnimationMixerPlayable Mixer => Behaviour.Mixer;

			public int CurrentKey
			{
				get => Behaviour.CurrentKey;
				set => Behaviour.CurrentKey = value;
			}
		}

		private struct OperationHandleData
		{
			public int Index;
		}
	}
}