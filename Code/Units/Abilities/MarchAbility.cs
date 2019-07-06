using package.patapon.core;
using package.patapon.core.Animation;
using package.StormiumTeam.GameBase;
using Patapon4TLB.Core;
using Patapon4TLB.UI.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Patapon4TLB.Default
{
	public struct MarchAbility : IComponentData
	{
		public float AccelerationFactor;
	}

	[UpdateInGroup(typeof(ActionSystemGroup))]
	public class MarchAbilitySystem : JobGameBaseSystem
	{
		private struct JobProcess : IJobForEachWithEntity<Owner, RhythmAbilityState, MarchAbility>
		{
			public float DeltaTime;

			[ReadOnly] public ComponentDataFromEntity<Translation>        TranslationFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitRhythmState>    UnitStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<GroundState>        GroundStateFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitBaseSettings>   UnitSettingsFromEntity;
			[ReadOnly] public ComponentDataFromEntity<UnitTargetPosition> UnitTargetPositionFromEntity;

			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<UnitControllerState> UnitControllerStateFromEntity;
			[NativeDisableParallelForRestriction] public ComponentDataFromEntity<Velocity>            VelocityFromEntity;

			public void Execute(Entity entity, int _, [ReadOnly] ref Owner owner, [ReadOnly] ref RhythmAbilityState state, [ReadOnly] ref MarchAbility marchAbility)
			{
				if (!state.IsActive)
					return;

				var unitSettings   = UnitSettingsFromEntity[owner.Target];
				var targetPosition = UnitTargetPositionFromEntity[owner.Target];
				var groundState    = GroundStateFromEntity[owner.Target];

				if (!groundState.Value)
					return;

				var combo    = UnitStateFromEntity[owner.Target].Combo;
				var velocity = VelocityFromEntity[owner.Target];

				// to not make tanks op, we need to get the weight from entity and use it as an acceleration factor
				var acceleration = math.clamp(math.rcp(unitSettings.Weight), 0, 1) * marchAbility.AccelerationFactor * 50;
				acceleration = math.min(acceleration * DeltaTime, 1);

				var walkSpeed = unitSettings.BaseWalkSpeed;
				if (combo.IsFever)
				{
					walkSpeed = unitSettings.FeverWalkSpeed;
				}

				var direction = System.Math.Sign(targetPosition.Value.x - TranslationFromEntity[owner.Target].Value.x);

				velocity.Value.x                 = math.lerp(velocity.Value.x, walkSpeed * direction, acceleration);
				VelocityFromEntity[owner.Target] = velocity;

				var controllerState = UnitControllerStateFromEntity[owner.Target];
				controllerState.ControlOverVelocity.x       = true;
				UnitControllerStateFromEntity[owner.Target] = controllerState;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (!IsServer)
				return inputDeps;

			return new JobProcess
			{
				DeltaTime                     = GetSingleton<GameTimeComponent>().DeltaTime,
				UnitStateFromEntity           = GetComponentDataFromEntity<UnitRhythmState>(true),
				UnitSettingsFromEntity        = GetComponentDataFromEntity<UnitBaseSettings>(true),
				TranslationFromEntity         = GetComponentDataFromEntity<Translation>(true),
				GroundStateFromEntity         = GetComponentDataFromEntity<GroundState>(true),
				UnitTargetPositionFromEntity  = GetComponentDataFromEntity<UnitTargetPosition>(true),
				UnitControllerStateFromEntity = GetComponentDataFromEntity<UnitControllerState>(),
				VelocityFromEntity            = GetComponentDataFromEntity<Velocity>()
			}.Schedule(this, inputDeps);
		}
	}

	[UpdateInGroup(typeof(ClientUnitAnimationGroup))]
	public class MarchAbilityClientAnimationSystem : GameBaseSystem
	{
		private class SystemPlayable : PlayableBehaviour
		{
			public Playable               Self;
			public AnimationMixerPlayable Mixer;
			public AnimationMixerPlayable Root;

			public int   TargetAnimation;
			public float Weight;

			public void Initialize(Playable self, int index, PlayableGraph graph, AnimationMixerPlayable rootMixer, AnimationClip[] clips)
			{
				Self = self;
				Root = rootMixer;
				
				Mixer = AnimationMixerPlayable.Create(graph, clips.Length, true);
				Mixer.SetPropagateSetTime(true);

				for (var i = 0; i != clips.Length; i++)
				{
					var clipPlayable = AnimationClipPlayable.Create(graph, clips[i]);

					graph.Connect(clipPlayable, 0, Mixer, i);
				}

				rootMixer.AddInput(self, index, 1);
				self.AddInput(Mixer, 0, 1);
			}

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				var inputCount = Mixer.GetInputCount();
				for (var i = 0; i != inputCount; i++)
				{
					Mixer.SetInputWeight(i, i == TargetAnimation ? 1 : 0);
					if (i == TargetAnimation)
						Mixer.GetInput(i).Play();
					else
						Mixer.GetInput(i).Pause();
				}

				Root.SetInputWeight(VisualAnimation.GetIndexFrom(Root, Self), Weight);
			}
		}

		private struct SystemData
		{
			public ScriptPlayable<SystemPlayable> Playable;

			public AnimationMixerPlayable Mixer
			{
				get => Playable.GetBehaviour().Mixer;
			}

			public int TargetAnimation
			{
				get => Playable.GetBehaviour().TargetAnimation;
				set => Playable.GetBehaviour().TargetAnimation = value;
			}

			public float Weight
			{
				get => Playable.GetBehaviour().Weight;
				set => Playable.GetBehaviour().Weight = value;
			}
		}

		private struct OperationHandleData
		{
			public bool IsAttackAnimation;
		}

		private AnimationClip m_MarchAnimationClip;
		private AnimationClip m_MarchAttackAnimationClip;


		private AsyncOperationModule m_AsyncOperationModule;

		private EntityQuery                                                     m_MarchAbilitiesQuery;
		private EntityQueryBuilder.F_CC<UnitVisualBackend, UnitVisualAnimation> m_ForEachDelegate;

		private NativeArray<Entity> m_MarchEntitiesAbilities;
		private NativeArray<Owner>  m_MarchOwnerAbilities;

		private const string AddrKey = "char_anims/{0}.anim";

		protected override void OnCreate()
		{
			base.OnCreate();

			m_MarchAbilitiesQuery = GetEntityQuery(typeof(MarchAbility), typeof(Owner));
			m_ForEachDelegate     = ForEach;
			GetModule(out m_AsyncOperationModule);

			m_AsyncOperationModule.Add(Addressables.LoadAssetAsync<AnimationClip>(string.Format(AddrKey, "Walking")), new OperationHandleData {IsAttackAnimation = false});
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOperationModule.Handles.Count; i++)
			{
				var handle = m_AsyncOperationModule.Handles[i].Handle;
				if (handle.Result != null)
				{
					m_MarchAnimationClip = handle.Convert<AnimationClip>().Result;
					m_AsyncOperationModule.Handles.RemoveAtSwapBack(i);
					i--;
				}
			}

			if (m_MarchAnimationClip == null)
				return;

			m_MarchEntitiesAbilities = m_MarchAbilitiesQuery.ToEntityArray(Allocator.TempJob);
			m_MarchOwnerAbilities    = m_MarchAbilitiesQuery.ToComponentDataArray<Owner>(Allocator.TempJob);
			{
				Entities.ForEach(m_ForEachDelegate);
			}
			m_MarchEntitiesAbilities.Dispose();
			m_MarchOwnerAbilities.Dispose();
		}

		private void AddAnimation(ref VisualAnimation.ManageData data, ref SystemData systemData)
		{
			var playable = ScriptPlayable<SystemPlayable>.Create(data.Graph);
			var behavior = playable.GetBehaviour();
			
			behavior.Initialize(playable, data.Index, data.Graph, data.Behavior.RootMixer, new[]
			{
				m_MarchAnimationClip,
				// m_MarchAttackAnimationClip // not yet
			});

			systemData.Playable = playable;
		}

		private void RemoveAnimation(VisualAnimation.ManageData manageData, SystemData systemData)
		{

		}

		private void ForEach(UnitVisualBackend backend, UnitVisualAnimation animation)
		{
			var currAnim   = animation.CurrAnimation;
			var systemType = GetType();

			if (currAnim.Type != systemType && !currAnim.AllowOverride)
			{
				if (animation.ContainsSystem(systemType))
				{
					animation.GetSystemData<SystemData>(systemType).Weight = 0;
				}

				return;
			}

			Entity abilityEntity = default;
			for (var ab = 0; ab != m_MarchEntitiesAbilities.Length; ab++)
			{
				if (m_MarchOwnerAbilities[ab].Target == backend.DstEntity)
					abilityEntity = m_MarchEntitiesAbilities[ab];
			}

			if (abilityEntity == default && currAnim.Type == systemType || currAnim.CanStartAnimationAt(animation.RootTime))
			{
				animation.SetTargetAnimation(new TargetAnimation(null, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd));
			}

			if (!animation.ContainsSystem(systemType))
			{
				animation.InsertSystem<SystemData>(systemType, AddAnimation, RemoveAnimation);
			}

			var systemData  = animation.GetSystemData<SystemData>(systemType);
			var doAnimation = currAnim == TargetAnimation.Null || currAnim.Type == systemType;

			if (abilityEntity != default)
			{
				var abilityState = EntityManager.GetComponentData<RhythmAbilityState>(abilityEntity);
				doAnimation |= abilityState.IsActive;
			}

			if (doAnimation)
			{
				systemData.TargetAnimation = 0;
				systemData.Weight = 1 - currAnim.GetTransitionWeightFixed(animation.RootTime);
				
				animation.SetTargetAnimation(new TargetAnimation(systemType, transitionStart: currAnim.TransitionStart, transitionEnd: currAnim.TransitionEnd));
			}
			else
			{
				systemData.Weight = 0;
			}
		}
	}

	public class MarchAbilityProvider : BaseProviderBatch<MarchAbilityProvider.Create>
	{
		public struct Create
		{
			public Entity Owner;
			public Entity Command;
			public float  AccelerationFactor;
		}

		public override void GetComponents(out ComponentType[] entityComponents)
		{
			entityComponents = new ComponentType[]
			{
				typeof(ActionDescription),
				typeof(RhythmAbilityState),
				typeof(MarchAbility),
				typeof(Owner),
				typeof(DestroyChainReaction)
			};
		}

		public override void SetEntityData(Entity entity, Create data)
		{
			EntityManager.ReplaceOwnerData(entity, data.Owner);
			EntityManager.SetComponentData(entity, new RhythmAbilityState {Command      = data.Command});
			EntityManager.SetComponentData(entity, new MarchAbility {AccelerationFactor = data.AccelerationFactor});
			EntityManager.SetComponentData(entity, new Owner {Target                    = data.Owner});
			EntityManager.SetComponentData(entity, new DestroyChainReaction(data.Owner));
		}
	}
}