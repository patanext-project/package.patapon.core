using DefaultNamespace;
using package.stormiumteam.shared.ecs;
using Patapon.Client.Graphics.Animation.Units;
using Patapon.Mixed.GamePlay;
using Patapon.Mixed.GamePlay.Abilities.CTate;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace package.patapon.core.Animation.Units.CTate
{
	public class BasicTaterazayDefendAbilityAnimation : BaseAbilityAnimationSystem
	<
		BasicTaterazayDefendAbilityAnimation.SystemPlayable,
		BasicTaterazayDefendAbilityAnimation.PlayableInitData,
		BasicTaterazayDefendAbilityAnimation.SystemData
	>
	{
		private readonly AddressBuilderClient m_AddrPath = AddressBuilder.Client()
		                                                                 .Folder("Models")
		                                                                 .Folder("UberHero")
		                                                                 .Folder("Animations")
		                                                                 .Folder("Taterazay");

		private AnimationClip m_AnimationClip;

		private int m_LoadSuccess;

		protected override void OnCreate()
		{
			base.OnCreate();

			LoadAssetAsync<AnimationClip, OperationHandleData>(m_AddrPath.GetFile("TaterazayBasicDefend.anim"), new OperationHandleData());
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
			EntityManager.TryGetComponentData<RhythmAbilityState>(abilityEntity, out var abilityState);
			if (abilityEntity == default || !abilityState.IsActive)
			{
				if (currAnim.Type == SystemType && !abilityState.IsStillChaining)
					animation.SetTargetAnimation(new TargetAnimation(default, previousType: currAnim.Type));

				return;
			}

			if (!currAnim.AllowOverride || (currAnim.Type != SystemType && abilityState.CanBeTransitioned))
				return;

			ResetIdleTime(targetEntity);
			InjectAnimation(animation, new PlayableInitData {Clip = m_AnimationClip});

			animation.SetTargetAnimation(new TargetAnimation(SystemType, allowOverride: true, stopAt: animation.RootTime + 0.25));
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(RhythmAbilityState), typeof(Owner)},
				Any = new ComponentType[] {typeof(BasicTaterazayDefendAbility), typeof(BasicTaterazayDefendFrontalAbility)}
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
		}

		public class SystemPlayable : BaseAbilityPlayable<PlayableInitData>
		{
			public float Weight;

			protected override void OnInitialize(PlayableInitData init)
			{
				var clipPlayable = AnimationClipPlayable.Create(Graph, init.Clip);
				Mixer.ConnectInput(0, clipPlayable, 0, 1);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global   = (float) Root.GetTime();
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