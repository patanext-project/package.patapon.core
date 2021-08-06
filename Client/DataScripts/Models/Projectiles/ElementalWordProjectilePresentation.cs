using System;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Systems.PoolingSystems;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.GamePlay.Projectiles;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace PataNext.Client.DataScripts.Models.Projectiles
{
	public class ElementalWordProjectilePresentation : EntityVisualPresentation
	{
		[Serializable]
		public struct Trigger
		{
			public Animator[] animators;
			public string     trigger;

			public void Set()
			{
				foreach (var animator in animators)
					animator.SetTrigger(trigger);
			}
		}

		public enum EPhase
		{
			NotInit,
			Idle,
			Explosion
		}

		[Header("Properties")]
		public float poolingDelayBeforeAfterExplosion = 2f;

		public Transform rotObject;

		[Header("Animator Parameters")]
		public Trigger onIdle = new Trigger {trigger = "OnIdle", animators = new Animator[0]};

		public Trigger onExplosion = new Trigger {trigger = "OnExplosion", animators = new Animator[0]};

		private EPhase m_PreviousPhase;

		public override void OnReset()
		{
			base.OnReset();

			m_PreviousPhase    = EPhase.NotInit;
			CurrentPoolingTime = -1;
		}

		public override void OnBackendSet()
		{
			base.OnBackendSet();

			((EntityVisualBackend) Backend).letPresentationUpdateTransform = true;
			((EntityVisualBackend) Backend).canBePooled                    = false;
			
			GetSortingGroup().sortingLayerName = "BattlegroundEffects";
		}

		public void SetPhase(EPhase phase)
		{
			if (phase != m_PreviousPhase)
			{
				m_PreviousPhase = phase;
				switch (phase)
				{
					case EPhase.NotInit:
						break;
					case EPhase.Idle:
						onIdle.Set();
						foreach (var animator in onIdle.animators) animator.Update(0.001f);
						
						break;
					case EPhase.Explosion:
						onExplosion.Set();
						foreach (var animator in onExplosion.animators) animator.Update(0.001f);
						
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
				}
			}
		}

		public EPhase GetCurrentPhase() => m_PreviousPhase;

		public float CurrentPoolingTime { get; set; }
		public bool  CanBePooled        => CurrentPoolingTime >= poolingDelayBeforeAfterExplosion;

		internal float3     pos;
		internal quaternion rot;
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	[UpdateAfter(typeof(EntityVisualPoolingSystem))]
	public class ElementalWordProjectileRenderSystem : BaseRenderSystem<ElementalWordProjectilePresentation>
	{
		protected override void PrepareValues()
		{

		}

		protected override void Render(ElementalWordProjectilePresentation definition)
		{
			var backend = definition.Backend as EntityVisualBackend;
			var entity  = backend.DstEntity;

			var phase = definition.GetCurrentPhase();
			if (EntityManager.Exists(entity))
			{
				phase = ElementalWordProjectilePresentation.EPhase.Idle;
				if (EntityManager.HasComponent<ProjectileExplodedEndReason>(entity))
					phase = ElementalWordProjectilePresentation.EPhase.Explosion;

				if (EntityManager.TryGetComponentData(entity, out Translation translation))
					definition.pos = translation.Value;

				definition.rot = math.mul(definition.rot, quaternion.Euler(0, 0, 10 * Time.DeltaTime));
			}
			else
			{
				definition.CurrentPoolingTime += Time.DeltaTime;
				phase                         =  ElementalWordProjectilePresentation.EPhase.Explosion;
			}

			definition.SetPhase(phase);
			backend.transform.localPosition = definition.pos;
			if (phase != ElementalWordProjectilePresentation.EPhase.Explosion)
				definition.rotObject.Rotate(0, 0, 360 * Time.DeltaTime);

			if (definition.CurrentPoolingTime >= definition.poolingDelayBeforeAfterExplosion)
				backend.canBePooled = true;
		}

		protected override void ClearValues()
		{

		}
	}
}