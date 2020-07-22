using Patapon.Client.Graphics.Animation.Units;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace package.patapon.core.Animation.Units.CYari
{
	public class BasicYaridaAttackAbilityAnimation : BaseAbilityAnimationSystem
	<
		BasicYaridaAttackAbilityAnimation.SystemPlayable,
		BasicYaridaAttackAbilityAnimation.PlayableInitData,
		BasicYaridaAttackAbilityAnimation.SystemData
	>
	{
		private AnimationClip m_AnimationClip;
		private int           m_LoadSuccess;

		protected override void OnCreate()
		{
			base.OnCreate();

			var animationFolder = AddressBuilder.Client()
			                                    .Folder("Models")
			                                    .Folder("UberHero")
			                                    .Folder("Animations")
			                                    .Folder("Yarida");

			LoadAssetAsync<AnimationClip, HandleOp>(animationFolder.GetFile("YaridaBasicAttack.anim"), default);
		}

		protected override EntityQuery GetAbilityQuery()
		{
			return GetEntityQuery(typeof(BasicYaridaAttackAbility), typeof(Owner));
		}

		protected override void OnAsyncOpUpdate(ref int index)
		{
			var (handle, data) = DefaultAsyncOperation.InvokeExecute<AnimationClip, HandleOp>(AsyncOp, ref index);
			if (handle.Result == null)
				return;

			m_AnimationClip = handle.Result;
			m_LoadSuccess++;
		} 

		protected override bool OnBeforeForEach()
		{
			return base.OnBeforeForEach() && m_LoadSuccess >= 1;
		}

		protected override void OnUpdate(Entity targetEntity, UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim = animation.CurrAnimation;
			if (currAnim.Type == SystemType && currAnim.StopAt < animation.RootTime)
			{
				// allow transitions and overrides now...
				animation.SetTargetAnimation(new TargetAnimation(currAnim.Type, transitionStart: currAnim.StopAt, transitionEnd: currAnim.StopAt + 0.15));
				// if no one set another animation, then let's set to null...
				if (animation.RootTime > currAnim.StopAt + 0.9)
					animation.SetTargetAnimation(TargetAnimation.Null);
			}

			var abilityEntity = AbilityFinder.GetAbility(backend.DstEntity);
			if (abilityEntity == default)
				return;

			var gameTick      = GetTick(true);
			var attackAbility = EntityManager.GetComponentData<BasicYaridaAttackAbility>(abilityEntity);
			if (attackAbility.AttackStartTick <= 0)
				return;

			ResetIdleTime(targetEntity);
			InjectAnimation(animation, new PlayableInitData {Clip = m_AnimationClip});

			ref var systemData = ref animation.GetSystemData<SystemData>(SystemType);
			if (attackAbility.AttackStartTick == systemData.PreviousAttackStartTick)
				return;

			var aheadStartDifference = UTick.CopyDelta(gameTick, math.max(gameTick.Value - attackAbility.AttackStartTick, 0));

			systemData.PreviousAttackStartTick = attackAbility.AttackStartTick;
			systemData.Behaviour.StartTime     = animation.RootTime - math.clamp(aheadStartDifference.Seconds, -0.2, 0.2);

			animation.SetTargetAnimation(new TargetAnimation(SystemType, false, false, stopAt: animation.RootTime + 0.9));
		}

		public struct PlayableInitData
		{
			public AnimationClip Clip;
		}

		public struct SystemData : IPlayableSystemData<SystemPlayable>
		{
			public SystemPlayable Behaviour { get; set; }
			public uint           PreviousAttackStartTick;
		}

		public class SystemPlayable : BaseAbilityPlayable<PlayableInitData>
		{
			public double StartTime;
			public float  Weight;

			protected override void OnInitialize(PlayableInitData init)
			{
				var clipPlayable = AnimationClipPlayable.Create(Graph, init.Clip);
				Mixer.ConnectInput(0, clipPlayable, 0, 1);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var global   = (float) (Root.GetTime() - StartTime);
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

		public struct HandleOp
		{
		}
	}
}