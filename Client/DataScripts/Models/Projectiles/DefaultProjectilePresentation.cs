using System;
using package.stormiumteam.shared.ecs;
using PataNext.Client.Systems;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Utility.Rendering.BaseSystems;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PataNext.Client.DataScripts.Models.Projectiles
{
	public class DefaultProjectilePresentation : BaseProjectilePresentation
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

		[Header("Animator Parameters")]
		public Trigger onIdle = new Trigger {trigger = "OnIdle", animators = new Animator[0]};

		public Trigger onExplosion = new Trigger {trigger = "OnExplosion", animators = new Animator[0]};

		[Header("Sounds")]
		public AudioClip[] soundOnExplosion;

		public ECSoundEmitterComponent soundOnExplosionEmitter = new ECSoundEmitterComponent
		{
			volume       = 1,
			spatialBlend = 0,
			position     = 0,
			minDistance  = 10,
			maxDistance  = 20,
			rollOf       = AudioRolloffMode.Logarithmic
		};
		
		private EPhase m_PreviousPhase;

		public override void OnReset()
		{
			base.OnReset();

			m_PreviousPhase = EPhase.NotInit;
			CurrentPoolingTime = -1;
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
						break;
					case EPhase.Explosion:
						onExplosion.Set();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
				}
			}
		}

		public EPhase GetCurrentPhase() => m_PreviousPhase;

		public float CurrentPoolingTime { get; set; }
		public bool  CanBePooled        => CurrentPoolingTime >= poolingDelayBeforeAfterExplosion;
	}

	[UpdateInGroup(typeof(OrderGroup.Presentation.AfterSimulation))]
	public class RenderSystem : BaseRenderSystem<DefaultProjectilePresentation>
	{
		protected override void PrepareValues()
		{

		}

		protected override void Render(DefaultProjectilePresentation definition)
		{
			var backend = definition.Backend as DefaultProjectileBackend;
			var entity  = backend.DstEntity;

			var phase = definition.GetCurrentPhase();
			if (EntityManager.Exists(entity))
			{
				phase = DefaultProjectilePresentation.EPhase.Idle;
				if (EntityManager.HasComponent<ProjectileExplodedEndReason>(entity))
					phase = DefaultProjectilePresentation.EPhase.Explosion;

				if (EntityManager.TryGetComponentData(entity, out Translation translation))
					backend.pos = translation.Value;
				if (EntityManager.TryGetComponentData(entity, out SVelocity velocity))
				{
					var dir   = velocity.normalized;
					var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
					backend.rot = Quaternion.AngleAxis(angle, Vector3.forward);
				}
			}
			else
			{
				definition.CurrentPoolingTime += Time.DeltaTime;
			}

			// do explosion sound
			if (definition.GetCurrentPhase() != phase
			    && phase == DefaultProjectilePresentation.EPhase.Explosion
			    && definition.soundOnExplosion != null)
			{
				var sound = definition.soundOnExplosion[Random.Range(0, definition.soundOnExplosion.Length)];
				var soundDef = World.GetExistingSystem<ECSoundSystem>().ConvertClip(sound);
				if (soundDef.IsValid)
				{
					var soundEntity = EntityManager.CreateEntity(typeof(ECSoundEmitterComponent), typeof(ECSoundDefinition), typeof(ECSoundOneShotTag));
					var emitter     = definition.soundOnExplosionEmitter;
					emitter.position = backend.pos;

					EntityManager.SetComponentData(soundEntity, emitter);
					EntityManager.SetComponentData(soundEntity, soundDef);
				}
			}

			definition.SetPhase(phase);
			backend.transform.SetPositionAndRotation(backend.pos, backend.rot);
		}

		protected override void ClearValues()
		{

		}
	}
}