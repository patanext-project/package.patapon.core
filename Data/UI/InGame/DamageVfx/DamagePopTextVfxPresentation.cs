using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.Shared.Gen;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Patapon4TLB.UI.InGame.DamageVfx
{
	public class DamagePopTextVfxPresentation : RuntimeAssetPresentation<DamagePopTextVfxPresentation>
	{
		public TextMeshPro[] DamageLabels;
		public Animator      Animator;
	}

	public class DamagePopTextVfxBackend : RuntimeAssetBackend<DamagePopTextVfxPresentation>
	{
		public bool              IsPlayQueued;
		public TargetDamageEvent Event;

		public float SetToPoolAt;

		public void Play(TargetDamageEvent damageEvent)
		{
			IsPlayQueued = true;
			Event        = damageEvent;
		}
	}

	public class DamagePopTextVfxSystem : ComponentSystem
	{
		private EntityQuery m_Query;

		private static readonly int OnHit = Animator.StringToHash("OnHit");

		protected override void OnCreate()
		{
			m_Query = GetEntityQuery(typeof(DamagePopTextVfxBackend), typeof(RuntimeAssetDisable));
		}

		protected override void OnUpdate()
		{
			DamagePopTextVfxBackend backend = default;
			RuntimeAssetDisable     disable = default;

			foreach (var (i, entity) in this.ToEnumerator_CD(m_Query, ref backend, ref disable))
			{
				if (backend.SetToPoolAt < Time.time)
				{
					disable.IgnoreParent       = true;
					disable.ReturnPresentation = true;
					disable.DisableGameObject  = true;
					disable.ReturnToPool       = true;
					continue;
				}

				if (!backend.IsPlayQueued)
					continue;
				if (backend.Presentation == null)
					continue;

				var presentation = backend.Presentation;
				foreach (var label in presentation.DamageLabels)
				{
					label.text = backend.Event.Damage.ToString();
				}

				presentation.Animator.SetTrigger(OnHit);

				backend.IsPlayQueued = false;
				backend.transform.position = new Vector3
				{
					x = backend.Event.Position.x,
					y = backend.Event.Position.y,
					z = -10
				};
			}
		}
	}
}