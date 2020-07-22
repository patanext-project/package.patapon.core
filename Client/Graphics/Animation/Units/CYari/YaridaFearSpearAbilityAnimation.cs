using System.Collections.Generic;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units.CYari
{
	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	[UpdateAfter(typeof(UnitPressureClientAnimation))]
	[UpdateBefore(typeof(MarchAbilityClientAnimationSystem))]
	public class YaridaFearSpearAbilityAnimation : BaseAbilityAnimationSystem
	<
		YaridaFearSpearAbilityAnimation.SystemPlayable,
		YaridaFearSpearAbilityAnimation.PlayableInitData,
		YaridaFearSpearAbilityAnimation.SystemData
	>
	{
		private readonly AddressBuilderClient m_AddrPath = AddressBuilder.Client()
		                                                                 .Folder("Models")
		                                                                 .Folder("UberHero")
		                                                                 .Folder("Animations")
		                                                                 .Folder("Yarida")
		                                                                 .Folder("FearSpear");

		private Dictionary<ETarget, AnimationClip> m_Clips;

		private int m_LoadSuccess;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Clips = new Dictionary<ETarget, AnimationClip>();
			LoadAssetAsync<AnimationClip, DataOp>(m_AddrPath.GetFile("activation.anim"), new DataOp
			{
				Type = ETarget.Activation
			});
			LoadAssetAsync<AnimationClip, DataOp>(m_AddrPath.GetFile("loop_idle.anim"), new DataOp
			{
				Type = ETarget.Attack
			});
			LoadAssetAsync<AnimationClip, DataOp>(m_AddrPath.GetFile("loop_walk.anim"), new DataOp
			{
				Type = ETarget.Walk
			});
		}

		protected override void OnAsyncOpUpdate(ref int index)
		{
			var (handle, data) = DefaultAsyncOperation.InvokeExecute<AnimationClip, DataOp>(AsyncOp, ref index);
			if (handle.Result == null)
				return;

			m_Clips[data.Type] = handle.Result;
			m_LoadSuccess++;
		}

		protected override bool OnBeforeForEach()
		{
			return base.OnBeforeForEach() && AsyncOp.Handles.Count == 0;
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim      = animation.CurrAnimation;
			var abilityEntity = AbilityFinder.GetAbility(backend.DstEntity);
			EntityManager.TryGetComponentData<AbilityState>(abilityEntity, out var abilityState);
			EntityManager.TryGetComponentData<AbilityEngineSet>(abilityEntity, out var engineSet);
			if (abilityEntity == default || (abilityState.Phase & (EAbilityPhase.HeroActivation | EAbilityPhase.ActiveOrChaining)) == 0)
			{
				if (currAnim.Type == SystemType)
					animation.SetTargetAnimation(new TargetAnimation(default, previousType: currAnim.Type));
				return;
			}

			ResetIdleTime(targetEntity);

			InjectAnimation(animation, new PlayableInitData {Clips = m_Clips});

			var abilityData = EntityManager.GetComponentData<YaridaFearSpearAbility>(abilityEntity);

			ref var systemData = ref animation.GetSystemData<SystemData>(SystemType);

			var phase = ETarget.Activation;
			if ((abilityState.Phase & EAbilityPhase.ActiveOrChaining) != 0)
			{
				if (abilityData.AttackStartTick > 0)
					phase = ETarget.Attack;
				else
					phase = ETarget.Walk;
			}

			systemData.Behaviour.TargetAnimation = phase;
			if (phase != ETarget.Walk)
			{
				animation.SetTargetAnimation(new TargetAnimation(SystemType, allowOverride: false));
			}
			else
			{
				animation.SetTargetAnimation(new TargetAnimation(typeof(MarchAbilityClientAnimationSystem), previousType: SystemType));
			}
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(YaridaFearSpearAbility), typeof(AbilityState), typeof(Owner)}
			});
		}

		public enum ETarget
		{
			Activation,
			Attack,
			Walk
		}

		public struct DataOp
		{
			public ETarget Type;
		}

		public struct PlayableInitData
		{
			public Dictionary<ETarget, AnimationClip> Clips;
		}

		public struct SystemData : IPlayableSystemData<SystemPlayable>
		{
			public SystemPlayable Behaviour { get; set; }
		}

		public class SystemPlayable : BaseAbilityPlayable<PlayableInitData>
		{
			private Dictionary<ETarget, AnimationClipPlayable> m_ClipPlayableMap;

			public float   Weight;
			public ETarget TargetAnimation;

			protected override void OnInitialize(PlayableInitData init)
			{
				m_ClipPlayableMap = new Dictionary<ETarget, AnimationClipPlayable>();
				foreach (var kvp in init.Clips)
				{
					var animPlayable = AnimationClipPlayable.Create(Graph, kvp.Value);
					Mixer.ConnectInput((int) kvp.Key, animPlayable, 0);
					m_ClipPlayableMap[kvp.Key] = animPlayable;
				}
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global   = (float) Root.GetTime();
				var currAnim = Visual.CurrAnimation;

				Mixer.SetTime(global);
				var inputCount = Mixer.GetInputCount();
				for (var i = 0; i != inputCount; i++)
				{
					Mixer.SetInputWeight(i, i == (int) TargetAnimation ? 1 : 0);
				}

				Weight = 0;
				if (currAnim.CanBlend(Root.GetTime()) && currAnim.PreviousType == SystemType)
					Weight = currAnim.GetTransitionWeightFixed(Root.GetTime());
				else if (currAnim.Type == SystemType)
					Weight = 1;

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}
		}
	}
}