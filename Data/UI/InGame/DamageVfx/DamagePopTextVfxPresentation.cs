using System;
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
using Random = UnityEngine.Random;

namespace Patapon4TLB.UI.InGame.DamageVfx
{
	public class DamagePopTextVfxPresentation : RuntimeAssetPresentation<DamagePopTextVfxPresentation>
	{
		public TextMeshPro[] DamageLabels;
		public Animator      Animator;

		private void OnEnable()
		{
			Animator.enabled = false;
		}
	}

	public class DamagePopTextVfxBackend : RuntimeAssetBackend<DamagePopTextVfxPresentation>
	{
		public bool              IsPlayQueued;
		public TargetDamageEvent Event;

		public float StartTime;
		public float SetToPoolAt;

		public void Play(TargetDamageEvent damageEvent)
		{
			IsPlayQueued = true;
			Event        = damageEvent;
		}
	}

	[UpdateInGroup(typeof(PresentationSystemGroup))]
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
				
				if (backend.Presentation == null)
					continue;

				var presentation = backend.Presentation;
				if (!backend.IsPlayQueued)
				{
					presentation.Animator.Update(Time.deltaTime);
					
					var count = (int)((Time.time - backend.StartTime) * 20);
					foreach (var label in presentation.DamageLabels)
					{
						label.maxVisibleCharacters = count;
						
						/*var textInfo = label.textInfo;
						for (var t = 0; t != textInfo.characterCount; t++)
						{
							var charInfo = textInfo.characterInfo[t];
							var meshInfo = textInfo.meshInfo[charInfo.materialReferenceIndex];
							
							var sourceTopLeft     = charInfo.topLeft;
							var sourceTopRight    = charInfo.topRight;
							var sourceBottomLeft  = charInfo.bottomLeft;
							var sourceBottomRight = charInfo.bottomRight;	

							var offset = (sourceTopLeft + sourceBottomRight) * 0.5f;
							
							var matrix = Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one * (1 + Time.time - backend.StartTime));

							var destinationTopLeft     = matrix.MultiplyPoint3x4(sourceTopLeft - offset) + offset;
							var destinationTopRight    = matrix.MultiplyPoint3x4(sourceTopRight - offset) + offset;
							var destinationBottomLeft  = matrix.MultiplyPoint3x4(sourceBottomLeft - offset) + offset;
							var destinationBottomRight = matrix.MultiplyPoint3x4(sourceBottomRight - offset) + offset;
							
							meshInfo.vertices[charInfo.vertexIndex + 1] = Vector3.one * 4; // TL
							meshInfo.vertices[charInfo.vertexIndex + 2] = destinationTopRight; // TR
							meshInfo.vertices[charInfo.vertexIndex + 0] = destinationBottomLeft; // BL
							meshInfo.vertices[charInfo.vertexIndex + 3] = Vector3.one * -4; // BR

							label.mesh.vertices = meshInfo.vertices;
						}*/
					}
					continue;
				}

				foreach (var label in presentation.DamageLabels)
				{
					label.text = backend.Event.Damage.ToString();
					label.maxVisibleCharacters = 0;
				}

				presentation.Animator.SetTrigger(OnHit);

				backend.IsPlayQueued = false;
				backend.StartTime = Time.time;
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