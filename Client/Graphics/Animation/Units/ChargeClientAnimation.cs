using package.stormiumteam.shared.ecs;
using PataNext.Client.Core.Addressables;
using PataNext.Client.Graphics.Animation.Base;
using PataNext.Client.Graphics.Animation.Units.Base;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PataNext.Client.Graphics.Animation.Units
{
	public class ChargeClientAnimation : BaseAbilityAnimationSystem
	<
		ChargeClientAnimation.SystemPlayable,
		ChargeClientAnimation.PlayableInitData,
		ChargeClientAnimation.SystemData
	>
	{
		private readonly AddressBuilderClient m_AddrPath = AddressBuilder.Client()
		                                                                 .Folder("Models")
		                                                                 .Folder("UberHero")
		                                                                 .Folder("Animations")
		                                                                 .Folder("Shared");

		private AnimationClip m_AnimationClip;

		private int m_LoadSuccess;

		protected override void OnCreate()
		{
			base.OnCreate();

			LoadAssetAsync<AnimationClip, OperationHandleData>(m_AddrPath.GetFile("Charge.anim"), new OperationHandleData());
		}

		protected override void OnAsyncOpUpdate(ref int index)
		{
			var (handle, data) = DefaultAsyncOperation.InvokeExecute<AnimationClip, OperationHandleData>(AsyncOp, ref index);
			if (handle.Result == null)
				return;

			m_AnimationClip = handle.Result;
			m_LoadSuccess++;
		}

		protected override bool OnBeforeForEach()
		{
			return base.OnBeforeForEach() && m_LoadSuccess != 0;
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type == SystemType && animation.RootTime > currAnim.StopAt)
				animation.SetTargetAnimation(new TargetAnimation(default, previousType: currAnim.Type));

			var abilityEntity = AbilityFinder.GetAbility(backend.DstEntity);
			EntityManager.TryGetComponentData<AbilityState>(abilityEntity, out var abilityState);
			EntityManager.TryGetComponentData<AbilityEngineSet>(abilityEntity, out var engineSet);
			if (abilityEntity == default || (abilityState.Phase & EAbilityPhase.Active) == 0)
			{
				if (currAnim.Type == SystemType && (abilityState.Phase & EAbilityPhase.Chaining) == 0)
					animation.SetTargetAnimation(new TargetAnimation(default, previousType: currAnim.Type));

				return;
			}

			var canBeTransitioned = engineSet.CommandState.IsInputActive(engineSet.Process.Milliseconds, engineSet.Settings.BeatInterval);
			if (!currAnim.AllowOverride || (currAnim.Type != SystemType && canBeTransitioned))
				return;

			ResetIdleTime(targetEntity);
			InjectAnimation(animation, new PlayableInitData {Clip = m_AnimationClip});

			animation.SetTargetAnimation(new TargetAnimation(SystemType, allowOverride: true, stopAt: animation.RootTime + 0.25));

			ref var systemData = ref animation.GetSystemData<SystemData>(SystemType);
			if (abilityState.UpdateVersion != systemData.ActivationId)
			{
				systemData.ActivationId = abilityState.UpdateVersion;
				systemData.Behaviour.StartTime = animation.RootTime;
			}
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(DefaultChargeAbility), typeof(AbilityState), typeof(Owner)}
			});
		}

		public struct OperationHandleData
		{
		}

		public struct PlayableInitData
		{
			public AnimationClip Clip;
		}

		public struct SystemData : IPlayableSystemData<SystemPlayable>
		{
			public SystemPlayable Behaviour { get; set; }
			public int ActivationId;
		}

		public class SystemPlayable : BaseAbilityPlayable<PlayableInitData>
		{
			public float  Weight;
			public double StartTime;

			protected override void OnInitialize(PlayableInitData init)
			{
				var clipPlayable = AnimationClipPlayable.Create(Graph, init.Clip);
				Mixer.ConnectInput(0, clipPlayable, 0, 1);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global   = (float) Root.GetTime() - StartTime;
				var currAnim = Visual.CurrAnimation;

				Mixer.SetTime(global);

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